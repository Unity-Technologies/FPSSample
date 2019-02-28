using UnityEngine;

public class InputSystem
{
    // TODO: these should be put in some global setting
    public static Vector2 s_JoystickLookSensitivity = new Vector2(90.0f, 60.0f);

    static float maxMoveYaw;
    static float maxMoveMagnitude;

    public void AccumulateInput(ref UserCommand command, float deltaTime)
    {
        // To accumulate move we store the input with max magnitude and uses that
        Vector2 moveInput = new Vector2(Game.Input.GetAxisRaw("Horizontal"), Game.Input.GetAxisRaw("Vertical"));
        float angle = Vector2.Angle(Vector2.up, moveInput);
        if (moveInput.x < 0)
            angle = 360 - angle;
        float magnitude = Mathf.Clamp(moveInput.magnitude, 0, 1);       
        if (magnitude > maxMoveMagnitude)
        {
            maxMoveYaw = angle;
            maxMoveMagnitude = magnitude;
        }
        command.moveYaw = maxMoveYaw;
        command.moveMagnitude = maxMoveMagnitude;

        float invertY = Game.configInvertY.IntValue > 0 ? -1.0f : 1.0f;

        Vector2 deltaMousePos = new Vector2(0, 0);
        if(deltaTime > 0.0f)
            deltaMousePos += new Vector2(Game.Input.GetAxisRaw("Mouse X"), Game.Input.GetAxisRaw("Mouse Y") * invertY);
        deltaMousePos += deltaTime * (new Vector2(Game.Input.GetAxisRaw("RightStickX") * s_JoystickLookSensitivity.x, - invertY * Game.Input.GetAxisRaw("RightStickY") * s_JoystickLookSensitivity.y));
        deltaMousePos += deltaTime * (new Vector2(
            ((Game.Input.GetKey(KeyCode.Keypad4) ? -1.0f : 0.0f) + (Game.Input.GetKey(KeyCode.Keypad6) ? 1.0f : 0.0f)) * s_JoystickLookSensitivity.x,
            - invertY * Game.Input.GetAxisRaw("RightStickY") * s_JoystickLookSensitivity.y));

        command.lookYaw += deltaMousePos.x * Game.configMouseSensitivity.FloatValue;
        command.lookYaw = command.lookYaw % 360;
        while (command.lookYaw < 0.0f) command.lookYaw += 360.0f;

        command.lookPitch += deltaMousePos.y * Game.configMouseSensitivity.FloatValue;
        command.lookPitch = Mathf.Clamp(command.lookPitch, 0, 180);

        command.buttons.Or(UserCommand.Button.Jump,Game.Input.GetKeyDown(KeyCode.Space) || Game.Input.GetKeyDown(KeyCode.Joystick1Button0)); 
        command.buttons.Or(UserCommand.Button.Boost,Game.Input.GetKey(KeyCode.LeftControl) || Game.Input.GetKey(KeyCode.Joystick1Button4));
        command.buttons.Or(UserCommand.Button.PrimaryFire, (Game.Input.GetMouseButton(0) && Game.GetMousePointerLock()) || (Game.Input.GetAxisRaw("Trigger") < -0.5f));
        command.buttons.Or(UserCommand.Button.SecondaryFire, Game.Input.GetMouseButton(1) || Game.Input.GetKey(KeyCode.Joystick1Button5));
        command.buttons.Or(UserCommand.Button.Ability1, Game.Input.GetKey(KeyCode.LeftShift));
        command.buttons.Or(UserCommand.Button.Ability2, Game.Input.GetKey(KeyCode.E));
        command.buttons.Or(UserCommand.Button.Ability3, Game.Input.GetKey(KeyCode.Q));
        command.buttons.Or(UserCommand.Button.Reload, Game.Input.GetKey(KeyCode.R) || Game.Input.GetKey(KeyCode.Joystick1Button2));
        command.buttons.Or(UserCommand.Button.Melee, Game.Input.GetKey(KeyCode.V) || Game.Input.GetKey(KeyCode.Joystick1Button1));
        command.buttons.Or(UserCommand.Button.Use, Game.Input.GetKey(KeyCode.E));

        command.emote = Game.Input.GetKeyDown(KeyCode.J) ? CharacterEmote.Victory : CharacterEmote.None;
        command.emote = Game.Input.GetKeyDown(KeyCode.K) ? CharacterEmote.Defeat : command.emote;
    }

    public void ClearInput(ref UserCommand command)     
    {
        maxMoveMagnitude = 0;
        command.ClearCommand();
    }
}
