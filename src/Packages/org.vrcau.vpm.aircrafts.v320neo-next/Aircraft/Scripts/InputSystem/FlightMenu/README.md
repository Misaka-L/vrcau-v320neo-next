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
├ MenuController   <系统>FlightMenuController（桥接 AvionicsBus，无外部引用；
│                  SeatAdjust 例外：seatAdjuster 是座位自己的 SeatAdjuster，
│                  由挂在该座位 station 上的 SeatAdjusterStationBinder 在本机玩家入座时写入）
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
| `SeatAdjustFlightMenu` | `Entries/Entry`（弹窗 "Seat Adjust"） | Desktop 的 Page 2、VR Left Hand（全机一份，入座时绑定座位，见下） |
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

机组菜单本体在 `v320neo-next/SaccEntity/Systems/FlightMenuGroup/Pilot/`
（`Desktop` / `Next Page` / `VR Left Hand` / `VR Right Hand`）。

## 机组菜单如何引用系统菜单

机组菜单节点的**子对象就是菜单内容本身**，运行时用的 `menuItems` 数组由子对象**扫描生成**：

- 系统条目在机组菜单下表现为一个 **`FlightMenuReferenceItem`** 子对象（挂在 `<系统>FlightMenu.prefab`
  实例外面，名字就是菜单标题，如 `Trim` / `A/THR` / `Park Brake`）。
  它只有一个 `targetMenuItem` 字段，指向 `EnableInVehicle/AvioncsFlightMenu/<系统>/Entries/<入口项>`。
  引用项自己的 title / eventTarget 等字段一律忽略。
- `Cabin Door`、`Landing Light`、`[Empty]`、`Next Page` 仍是本机自己的菜单项，直接挂在同一个组下面。
- **同级顺序 = 菜单顺序**。

举例（`Desktop`）：

```
Desktop                        FlightMenuGroup
├ Auto Start                   FlightMenuReferenceItem -> AutoStart/Entries/Entry
├ Cabin Door                   FlightMenuButtonItem    (本机)
├ Trim                         FlightMenuReferenceItem -> ElevatorTrim/Entries/Entry
├ VHF                          FlightMenuReferenceItem -> VHF/Entries/Entry
├ Landing Gear                 FlightMenuReferenceItem -> LandingGear/Entries/Entry
├ A/THR                        FlightMenuReferenceItem -> AutoThrust/Entries/Entry
├ Flaps                        FlightMenuReferenceItem -> Flap/Entries/Entry
├ Park Brake                   FlightMenuReferenceItem -> Brake/Entries/Entry
└ Next Page                    FlightMenuSubMenuItem + FlightMenuGroup
  ├ Landing Light              FlightMenuButtonItem    (本机)
  ├ Auto Brake                 FlightMenuReferenceItem -> AutoBrake/Entries/Entry
  └ Seat Adjust                FlightMenuReferenceItem -> SeatAdjust/Entries/Entry
```

## 给某个机组加/删/调整系统菜单

1. 在目标机组 `FlightMenuGroup` 下增删 `FlightMenuReferenceItem` 子对象，并把它们拖到想要的同级位置。
   - Inspector 上 `FlightMenuGroup` 的右键菜单里有 **Reference**，会新建一个空引用项；
     也可以复制现有的引用项再改 `targetMenuItem`。
   - 填 `targetMenuItem`：从 `EnableInVehicle/AvioncsFlightMenu/<系统>/Entries/` 里拖。
2. 选中该 `FlightMenuGroup`，按 Inspector 上的 **Scan child menu item update**，
   让 `menuItems` 重新由子对象生成（引用项会被展开成它指向的真实菜单项）。
3. 如果扫描不是通过 Inspector 做的（例如脚本调用
   `FlightMenuGroupEditor.ScanChildMenuItem(group)`），记得再调一次
   `UdonSharpEditorUtility.CopyProxyToUdon(group)` 同步 Udon 侧序列化。

**注意**：`Reference Item` 只在**编辑器扫描时**生效，运行期的 `FlightMenuView` 并不会解引用。
所以改完子对象一定要重新 Scan，否则运行时用的还是旧的 `menuItems`。

同一条目可以被多个机组共用：`FlightMenuItemBase` 是纯配置、无运行期状态
（滑块状态在 `FlightMenuSliderController`，它每个 MenuView 一份）。
因此一个 `Entries/Entry` 可以被多个机组的引用项同时指向，Controller 全机仍然只有一份。

## 给某个系统加一个新的入口变体

例：想让副驾驶用一个标题不同的 A/THR 入口。

1. 在 `AutoThrustFlightMenu.prefab` 的 `Entries/` 下复制 `Entry`，改名（如 `EntryFO`），改标题等参数。
2. 在副驾驶的 `FlightMenuGroup` 下加一个 `FlightMenuReferenceItem`，`targetMenuItem` 指向 `EntryFO`，
   然后 Scan。

这样每个机组可以有自己的入口参数，而 Controller 全机仍然只有一份。

## 座位调节（Seat Adjust）：共享一份实例，入座时绑定座位

`SeatAdjustFlightMenu` 与其它系统菜单的差别：它的 `MenuController`
（`SeatAdjusterFlightMenuController`）**不经过 AvionicsBus**，而是持有一个
`public SeatAdjuster seatAdjuster`，把 4 个 hold 事件转发给这个座位自己的
`SeatAdjuster`（`Aircraft/Scripts/Systems/Seat/SeatAdjuster.cs`）。
座位偏移是**每个 SeatAdjuster 自己的状态**，但菜单只需要全机一份：
`MenuController.seatAdjuster` 由**座位侧**的
`Aircraft/Scripts/InputSystem/FlightMenuController/Seat/SeatAdjusterStationBinder.cs`
在本机玩家入座时（`OnStationEntered` + `player.isLocal`）改写成该座位自己的 `SeatAdjuster`。

VRCStation 只把 `OnStationEntered` 发给**同一个 GameObject** 上的 UdonBehaviour，
所以 binder 挂在 station 根节点（例：`Seats/Cockpit/SeatPilot`），
不能挂在 SeatAdjuster 子节点或它的父节点上。
飞机 prefab 里 `MenuController.seatAdjuster` 的静态值保留为 SeatPilot 的 SeatAdjuster，
作为「还没入座」时的默认值；入座后由 binder 改写。

给飞机加/改一个座位的座位调节：

1. 复制 `SeatAdjuster` 节点到目标座位（现例：`Seats/SeatPilot/InSeatOnlyPilot/SeatAdjuster`），
   把 `adjustTargetInVr` / `adjustTargetInDesktop` 指向该座位的
   `StationEnterPlayerLocation` / `TargetEyePosition`；`moveSpeed`（按住移动速度，米/秒）与
   `adjustStep`（`StepMove*` 的步进距离）按该座位需要设置。
2. 把该节点放进该座位 `SaccVehicleSeat.EnableInSeat`，保持"本机只有所坐座位的
   SeatAdjuster 是启用的"这一前提。
3. 在该座位的 station 根节点上加一个 `SeatAdjusterStationBinder`：`menuController` 指向共享的
   `SeatAdjust` 实例里的 `MenuController`，`seatAdjuster` 指向步骤 1 的那个节点。
   （不要再复制 `SeatAdjust` 实例，也不需要按座位做 `MenuController.seatAdjuster` 外层覆盖。）
4. 在该座位的机组菜单下加一个 `FlightMenuReferenceItem`，`targetMenuItem` 指向共享实例的
   `Entries/Entry`，然后 Scan。（同一条目仍可被多个机组引用。）

补充：

- 座位调节不需要 AvionicsBus、也不读写任何总线数据 id，`SeatAdjuster` 可以直接复制到别的
  飞机/载具（复制脚本 + 菜单 prefab + binder 即可），每个座位互不影响。
- 座位回到初始位置：给 `SeatAdjuster` 发 `ResetAdjustment`。脚本**不自动归零**
  （原来由 AvionicsBus 的重生事件触发），重生/离座/换座位要不要归零由宿主决定。
- 菜单项 `eventTarget` 仍只指向自己 prefab 内的 `MenuController`；跨 prefab 的引用只有
  `seatAdjuster` 这一个（现在由 binder 在运行时写入，不再靠 prefab 外层覆盖）。
- binder 的 `menuController` / `seatAdjuster` 任一未赋值时，只在本地玩家入座时打一条 warning
  并保留旧绑定，不会把菜单指向空对象；非本地玩家的事件一律忽略。
- 只有自己带 `SeatAdjuster` 的座位才需要挂 binder。没挂 binder 的座位进入后，
  `MenuController.seatAdjuster` 仍是上一个入座座位写入的值（若该座位的机组菜单里也有
  Seat Adjust 条目，会去动那一个座位）；所以要么给它也加 `SeatAdjuster` + binder，
  要么不要在它的机组菜单里接 Seat Adjust 条目。

## 修改时的注意事项

- **`menuItems` 是派生的**：机组菜单的 `menuItems` 由子对象扫描生成，不要手工去改数组。
  改了子对象（增删、改顺序、改 `targetMenuItem`）就必须重新 Scan，否则运行期菜单不会变。
- **UdonSharp 双份序列化**：`FlightMenuGroup.menuItems` 等字段既存在于 proxy `MonoBehaviour`，
  也序列化进 `UdonBehaviour`（`publicVariablesUnityEngineObjects` + 变量 blob）。
  用脚本改完数组后必须调用 `UdonSharpEditorUtility.CopyProxyToUdon(...)`，否则运行时读到的还是旧值。
- **引用项会随构建发布**：`FlightMenuReferenceItem` 继承自 `FlightMenuItemBase`，不是 `IEditorOnly`，
  所以每个引用项在运行时也是一个（永远用不到的）UdonBehaviour。目前 4 个机组共 19 个。
  如果想省掉这部分开销，需要在 flight-menu 包里把它标记成编辑器专用。
- **prefab 之间不能互相指向内部对象**：prefab 资产只能引用别的**资产**。
  `VhfFlightMenu.prefab` 里 `RadioGroup/MenuController.transceiver` 是空的，真实值是在飞机 prefab 里
  对 `VHF` 实例做的**外层覆盖**（指向 `SaccEntity/Systems/ATA23-Communication/Radio/VHF/SFEXT_URC_VHF`）。
  重建实例后别忘了恢复这个覆盖。
  同理 `SeatAdjustFlightMenu.prefab` 里 `MenuController.seatAdjuster` 是空的；实际目标由挂在该座位
   station 上的 `SeatAdjusterStationBinder` 在本机玩家入座时写入（飞机 prefab 里的静态值只是
   入座前的默认值）。
- `VhfFlightMenu.prefab` 依赖 `org.vrcau.vpm.systems.flight-menu.integration.urc-redux` 的
  `MenuGroupVHF.prefab`；上游修 bug 后会自动跟着更新，不要把它内联展开。
- 改完 prefab 记得 `read_console` 查 error，并确认 `UdonSharpEditorUtility` 没有把引用丢成 null。
