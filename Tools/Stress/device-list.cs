// Play Mode probe: list Input System devices with pointer position/buttons, plus current pointer/mouse and update mode.
using UnityEngine.InputSystem;
using System.Linq;
var lines = InputSystem.devices.Select(d => {
    string extra = d is Pointer p ? $" pos={p.position.ReadValue()} press={p.press.isPressed}" : "";
    if (d is Mouse m) extra += $" middle={m.middleButton.isPressed}";
    return $"{d.GetType().Name} '{d.name}' id={d.deviceId}{extra}";
});
return string.Join("\n", lines) + $"\nPointer.current={Pointer.current?.name} Mouse.current={Mouse.current?.name} updateMode={InputSystem.settings.updateMode} bgBehavior={InputSystem.settings.backgroundBehavior} editorInput={InputSystem.settings.editorInputBehaviorInPlayMode}";
