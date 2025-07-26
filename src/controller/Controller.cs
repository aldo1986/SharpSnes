public class Controller
{
    // Un array para guardar el estado de cada botón
    private bool[] buttonStates = new bool[12]; // B, Y, Select, Start, Up, Down, Left, Right, A, X, L, R
    private int buttonIndex = 0;
    public ushort JoypadState = 0;

    public byte Read()
    {
        // Devuelve el estado del botón actual y avanza al siguiente
        if (buttonIndex < buttonStates.Length)
        {
            byte state = (byte)(buttonStates[buttonIndex] ? 1 : 0);
            buttonIndex++;
            return state;
        }
        return 1; // Por defecto, devuelve 1 cuando ya se leyeron todos los botones
    }

    public void Strobe()
    {
        // El "strobe" resetea el índice para empezar a leer desde el primer botón (B)
        buttonIndex = 0;
    }

    public void SetButtonState(int button, bool pressed)
    {
        if (button < buttonStates.Length)
        {
            buttonStates[button] = pressed;
        }
    }
}

// Un enum para facilitar el manejo de los botones
public enum SnesButton
{
    B, Y, Select, Start, Up, Down, Left, Right, A, X, L, R
}