# FlightMenu 系统菜单 prefab

这里存放 v320neo-next 各**系统**的飞行菜单。每个系统一个自包含 prefab：
子菜单组 + 组内菜单项 + 对应的 `*FlightMenuController` + 各机组用到的入口项。

飞机 prefab（`../../Prefab/v320neo-next.prefab`）里每个系统只有**一份共享实例**；
机组菜单（`Desktop` / `VR Left Hand` / `VR Right Hand`）的 `menuItems` 数组只引用这些实例里的入口项。

这样"给某个机组成员增删一个系统菜单"就只是改数组里的一个引用，不需要复制控件的 Controller。

## 目录

```
Aircraft/Scripts/InputSystem/FlightMenu/
├ AutoBrakeFlightMenu.prefab      自动刹车
├ VhfFlightMenu.prefab             VHF 收发机 + 频率输入
├ AutoThrustFlightMenu.prefab     A/THR
├ ElevatorTrimFlightMenu.prefab   俯仰配平
├ LandingGearFlightMenu.prefab    起落架
├ SeatAdjustFlightMenu.prefab     座椅调节
├ BrakeFlightMenu.prefab          停留刹车
├ AutoStartFlightMenu.prefab      自动启动
└ FlapFlightMenu.prefab           襟翼滑块
```

## prefab 内部布局

统一约定（`<Name>FlightMenu` 为根）：

```
<Name>FlightMenu
├ MenuController   <系统>FlightMenuController（桥接 AvionicsBus，无外部引用）
├ MenuGroup        FlightMenuGroup（该系统自己的子菜单；只有单条目系统没有这一层）
│ └ ...            组内菜单项
├ RadioGroup       （仅 VHF）MenuGroupVHF.prefab 的嵌套实例，含 FlightMenuRadioController 与频率输入组
└ Entries         各机组接进自己菜单用的入口项
  └ Entry / EntryTX / EntryMenu ...
```

当前入口项：

| prefab | 入口项 | 谁在用 |
| --- | --- | --- |
| `AutoBrakeFlightMenu` | `Entries/Entry`（弹窗子菜单 "Auto Brake"） | Desktop 的 Page 2、VR Right Hand |
| `VhfFlightMenu` | `Entries/Entry`（子菜单 "VHF"） | Desktop、VR Right Hand |
|  | `Entries/EntryTX`（按钮 "VHF TX"） | VR Left Hand |
| `AutoThrustFlightMenu` | `Entries/Entry`（弹窗 "A/THR"） | Desktop、VR Right Hand |
| `ElevatorTrimFlightMenu` | `Entries/Entry`（弹窗 "Trim"） | Desktop、VR Left Hand |
| `LandingGearFlightMenu` | `Entries/Entry`（按钮 "Landing Gear"） | Desktop |
|  | `Entries/EntryMenu`（弹窗 "Landing Gear"） | VR Right Hand |
| `SeatAdjustFlightMenu` | `Entries/Entry`（弹窗 "Seat Adjust"） | Desktop 的 Page 2、VR Left Hand |
| `BrakeFlightMenu` | `Entries/Entry`（按钮 "Park Brake"） | Desktop、VR Left Hand |
| `AutoStartFlightMenu` | `Entries/Entry`（按钮 "Auto Start"） | Desktop、VR Left Hand |
| `FlapFlightMenu` | `Entries/Entry`（滑块 "Flaps"） | Desktop、VR Right Hand |

> `LandingGearFlightMenu/Entries/EntryMenu` 同时挂着 `FlightMenuSubMenuItem` 和 `FlightMenuGroup`
> （它的弹出子菜单就是它自己下面的 `Cancel` / `Toggle Gear`）。这是从原飞机 prefab 继承下来的
> 双重身份，`Entry.subMenu` 自引用；拆成两个节点会需要在 prefab 内增删 UdonSharpBehaviour
> 组件，风险大于收益，故保留。

## 飞机 prefab 中的实例位置

```
v320neo-next/SaccEntity/Systems/EnableInVehicle/AvioncsFlightMenu/
├ AutoBrake  ├ VHF  ├ AutoThrust  ├ ElevatorTrim  ├ LandingGear
├ SeatAdjust ├ Brake ├ AutoStart   └ Flap
```

都在 `SaccEntity.EnableInVehicle` 门控下（进入驾驶舱才启用）。**VHF 必须在门控下**：
`FlightMenuRadioController.Start()` 会调用 `transceiver.OnUpdateChannel()`，
需要等 `SFEXT_URC_VHF` 先完成初始化。

机组菜单本体在 `v320neo-next/SaccEntity/Systems/FlightMenuGroup/Pilot/`，
里面只保留不属于任何系统的条目（`Cabin Door`、`Landing Light`、`[Empty]` 占位、`Next Page` 分组）。

## 给某个机组加/删系统菜单

1. 把 `EnableInVehicle/AvioncsFlightMenu/<系统>` 实例拖到该机组菜单节点下（若该实例已被删除），
   或直接复用现有实例。
2. 编辑该机组 `FlightMenuGroup.menuItems`，在想要的顺序位置插入/移除对
   `<系统>/Entries/<入口项>` 的引用。
3. 若新增了实例或改了数组，务必让 UdonSharp 重新序列化 proxy：
   `UdonSharpEditorUtility.CopyProxyToUdon(group)`（在 Inspector 里手动改会自动完成）。

同一条目可以被多个机组共用：`FlightMenuItemBase` 是纯配置、无运行期状态
（滑块状态在 `FlightMenuSliderController`，它每个 MenuView 一份）。

## 给某个系统加一个新的入口变体

例：想让副驾驶用一个标题不同的 A/THR 入口。

1. 在 `AutoThrustFlightMenu.prefab` 的 `Entries/` 下复制 `Entry`，改名（如 `EntryFO`），改标题等参数。
2. 在副驾驶的 `FlightMenuGroup.menuItems` 里引用它。

这样每个机组可以有自己的入口参数，而 Controller 全机仍然只有一份。

## 修改时的注意事项

- **UdonSharp 双份序列化**：`FlightMenuGroup.menuItems` 等字段既存在于 proxy `MonoBehaviour`，
  也序列化进 `UdonBehaviour`（`publicVariablesUnityEngineObjects` + 变量 blob）。
  用脚本改完数组后必须调用 `UdonSharpEditorUtility.CopyProxyToUdon(...)`，否则运行时读到的还是旧值。
- **prefab 之间不能互相指向内部对象**：prefab 资产只能引用别的**资产**。
  `VhfFlightMenu.prefab` 里 `RadioGroup/MenuController.transceiver` 是空的，真实值是在飞机 prefab 里
  对 `VHF` 实例做的**外层覆盖**（指向 `SaccEntity/Systems/ATA23-Communication/Radio/VHF/SFEXT_URC_VHF`）。
  重建实例后别忘了恢复这个覆盖。
- `VhfFlightMenu.prefab` 依赖 `org.vrcau.vpm.systems.flight-menu.integration.urc-redux` 的
  `MenuGroupVHF.prefab`；上游修 bug 后会自动跟着更新，不要把它内联展开。
- 改完 prefab 记得 `read_console` 查 error，并确认 `UdonSharpEditorUtility` 没有把引用丢成 null。
