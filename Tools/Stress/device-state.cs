// Play Mode probe: raw Input System device state (what the game sees this frame).
using UnityEngine.InputSystem;
var m = Mouse.current; var k = Keyboard.current;
return $"mouse pos={m?.position.ReadValue()} left={m?.leftButton.isPressed} middle={m?.middleButton.isPressed} | space={k?.spaceKey.isPressed} w={k?.wKey.isPressed}";
