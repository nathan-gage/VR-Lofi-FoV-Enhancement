using System;
using System.IO.Ports;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;


public class RGBLEDController
{
    private SerialPort _serialPort;

    public RGBLEDController(string comPort, int baudRate = 9600)
    {
        // Initialize serial connection
        _serialPort = new SerialPort(comPort, baudRate);
        _serialPort.Open();
    }

    public async Task SendRGBData(float[] rgbArray, float interval, int width, int height)
    {
        // Call the function to extract perimeter values
        List<int> perimeterValues = ExtractPerimeterValues(rgbArray, width, height);


        // Call the function to send interval value and perimeter values to Arduino
        await SendValuesToArduino(interval, perimeterValues);
    }

    public void Close()
    {
        // Close serial connection
        _serialPort.Close();
    }

    List<int> ExtractAndConvertRGBValues(float[] rgbArray, int index)
    {
        int R = (int)(rgbArray[index] * 255);
        int G = (int)(rgbArray[index + 1] * 255);
        int B = (int)(rgbArray[index + 2] * 255);
        return new List<int> { R, G, B };
    }

    List<int> ExtractPerimeterValues(float[] rgbArray, int width, int height)
    {
        List<int> perimeterValues = new List<int>();

        // Extract top row values
        for (int x = 0; x < width; x++)
        {
            int index = x * 3;
            perimeterValues.AddRange(ExtractAndConvertRGBValues(rgbArray, index));
        }

        // Extract right column values
        for (int y = 1; y < height; y++)
        {
            int index = (y * width + (width - 1)) * 3;
            perimeterValues.AddRange(ExtractAndConvertRGBValues(rgbArray, index));
        }

        // Extract bottom row values
        for (int x = width - 2; x >= 0; x--)
        {
            int index = ((height - 1) * width + x) * 3;
            perimeterValues.AddRange(ExtractAndConvertRGBValues(rgbArray, index));
        }

        // Extract left column values
        for (int y = height - 2; y > 0; y--)
        {
            int index = (y * width) * 3;
            perimeterValues.AddRange(ExtractAndConvertRGBValues(rgbArray, index));
        }

        return perimeterValues;

        // // Calculate rotation index
        // int rotationIndex = (width - 1) * 3 + (int)Math.Floor((height - 1) * 1.5);
        //
        // // Rotate the perimeterValues list
        // List<int> rotatedPerimeterValues = new List<int>();
        // rotatedPerimeterValues.AddRange(perimeterValues.GetRange(rotationIndex, perimeterValues.Count - rotationIndex));
        // rotatedPerimeterValues.AddRange(perimeterValues.GetRange(0, rotationIndex));
        //
        // return rotatedPerimeterValues;
    }

    private async Task SendValuesToArduino(float interval, List<int> perimeterValues)
    {
        // Serialize the interval value, number of RGB values, and add a start marker '<'
        string serializedData = $"<{interval.ToString()}|{perimeterValues.Count / 3}|";

        // Iterate through the list of perimeter RGB values and append to the serializedData string
        for (int i = 0; i < perimeterValues.Count; i += 3)
        {
            serializedData += $"{perimeterValues[i]},{perimeterValues[i + 1]},{perimeterValues[i + 2]}|";
        }

        // Add an end marker '>' to indicate the end of the transmission
        serializedData += ">";

        // Send the entire serialized data to Arduino via serial connection
        await _serialPort.BaseStream.WriteAsync(System.Text.Encoding.ASCII.GetBytes(serializedData), 0,
            serializedData.Length);
    }
}
