using LogicUI;
using System;
using System.Linq;
using System.Text;

class Program
{
    static protected int GetIntFromInputs(int startIdx, int numOfBits, int step = 1)
    {
        int result = 0;
        int halfInputCount = 10 / 2;

        for (int i = 0; i < numOfBits; i++)
        {
            int idx = (startIdx + i * step) % 10;
            if (i >= halfInputCount)
            {
                idx += 1;
            }
            Console.WriteLine(idx + " " + i);
        }
        return result;
    }
    static void Main()
    {
        GetIntFromInputs(0, 8, 2);
    }

}
