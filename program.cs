using System;
using System.IO;
using System.Timers;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

public class Program
{
    private static string confFile = "./conf.d";
    private static System.Timers.Timer aTimer;

    private static int maxSpeed = 100;
    private static int minSpeed = 20;
    private static int minimumRound = 5;
    private static int timerTime = 3000;
    private static int defaultFanSpeed = 40;

    private static string gpuFanLineStart = "GPU:";
    private static int gpuFan;

    private static string gpuMaxLineStart = "GPUMaxTemp:";
    private static int gpuMaxTemp;

    private static string gpuMinLineStart = "GPUMinTemp:";
    private static int gpuMinTemp;

    private static string cpuFanLineStart = "CPU:";
    private static int cpuFan;

    private static string cpuMaxLineStart = "CPUMaxTemp:";
    private static int cpuMaxTemp;

    private static string cpuMinLineStart = "CPUMinTemp:";
    private static int cpuMinTemp;

    private static float lastCPUSpeed = 0;
    private static float lastGPUSpeed = 0;

    public static void Main()
    {

        if(!Regex.IsMatch(DoGridCommand("get fan 1"), @"Fan 1: \d{1,5} RPM")){
            DoGridCommand("init");
        }

        DoGridCommand("set fans all speed " + defaultFanSpeed);

        ParseSpeedFromFile(confFile);

        SetTimer();
        Console.WriteLine("\nPress the Enter key to exit the application...\n");

        Console.ReadLine();
        aTimer.Stop();
        aTimer.Dispose();
        Console.WriteLine("Terminating the application...");
    }

    private static float GetGPUTemp(){
        ProcessStartInfo procStartInfo = new ProcessStartInfo("/bin/bash", "-c nvidia-smi -q -d temperature");
        procStartInfo.RedirectStandardOutput = true;
        procStartInfo.UseShellExecute = false;
        procStartInfo.CreateNoWindow = true;

        System.Diagnostics.Process proc = new System.Diagnostics.Process();
        proc.StartInfo = procStartInfo;
        proc.Start();
        string result = proc.StandardOutput.ReadToEnd();
        
        char first = '-';
        char second = '-';
        string temp = "";

        foreach (char c in result)
        {
            if(c=='1' || c == '2'|| c == '3'|| c == '4'|| c == '5'|| c == '6'|| c == '7'|| c == '8'|| c == '9'|| c == '0'|| c == 'C'){
                if(first == '-'){
                    first = c;
                }else if(second == '-'){
                    second = c;
                }else if(c == 'C'){
                    temp = "" + first + second;
                    break;
                }
            }else{
                first = '-';
                second = '-';
            }
        }
        return float.Parse(temp);
    }

    private static int GetCPUTemp(){
        string tempLineStart = "Physicalid0:+";

        ProcessStartInfo procStartInfo = new ProcessStartInfo("/bin/bash", "-c sensors");
        procStartInfo.RedirectStandardOutput = true;
        procStartInfo.UseShellExecute = false;
        procStartInfo.CreateNoWindow = true;

        System.Diagnostics.Process proc = new System.Diagnostics.Process();
        proc.StartInfo = procStartInfo;
        proc.Start();

        int temp = 0;
        string line = "";
        while ((line = proc.StandardOutput.ReadLine()) != null) {
            line = Regex.Replace(line, @"\s+", "");
            if(Regex.IsMatch(line, tempLineStart)){
                line = line.Trim().Replace(tempLineStart.Trim(), "");
                line = line.Substring(0, 6);
                line = Regex.Replace(line, "[^0-9.]", "");
                float t = float.Parse(line);
                temp = (int)t;
                break;
            }
        }

        return temp;
    }

    private static void SetTimer()
    {
        // Create a timer with a two second interval.
        aTimer = new System.Timers.Timer(timerTime);
        // Hook up the Elapsed event for the timer. 
        aTimer.Elapsed += OnTimedEvent;
        aTimer.AutoReset = true;
        aTimer.Enabled = true;
    }    

    private static void ParseSpeedFromFile(string fileName){
        StreamReader reader = File.OpenText(fileName);
        string line;    
        while ((line = reader.ReadLine()) != null) {
            Regex.Replace(line, @"\s+", "");
            if(Regex.IsMatch(line, gpuFanLineStart)){
                line = line.Trim().Replace(gpuFanLineStart, "");
                gpuFan = int.Parse(line);
            } else if(Regex.IsMatch(line, gpuMaxLineStart)){
                line = line.Trim().Replace(gpuMaxLineStart, "");
                gpuMaxTemp = int.Parse(line);
            } else if(Regex.IsMatch(line, gpuMinLineStart)){
                line = line.Trim().Replace(gpuMinLineStart, "");
                gpuMinTemp = int.Parse(line);
            } else if(Regex.IsMatch(line, cpuFanLineStart)){
                line = line.Trim().Replace(cpuFanLineStart, "");
                cpuFan = int.Parse(line);
            } else if(Regex.IsMatch(line, cpuMaxLineStart)){
                line = line.Trim().Replace(cpuMaxLineStart, "");
                cpuMaxTemp = int.Parse(line);
            } else if(Regex.IsMatch(line, cpuMinLineStart)){
                line = line.Trim().Replace(cpuMinLineStart, "");
                cpuMinTemp = int.Parse(line);
            }
        }
    }

    private static void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        float gpuTemp = GetGPUTemp();
        Console.WriteLine("GPU temp: " + gpuTemp);
        float gpuPercentage = 100 * ((gpuTemp - gpuMinTemp) / (gpuMaxTemp - gpuMinTemp));
        Console.WriteLine("GPU percentage: " + gpuPercentage);
        float gpuFanSpeed = gpuPercentage > maxSpeed ? maxSpeed : gpuPercentage < minSpeed ? minSpeed : gpuPercentage;
        gpuFanSpeed = (int) Math.Ceiling(gpuFanSpeed / minimumRound) * minimumRound;

        if(lastGPUSpeed != gpuFanSpeed){
            if(!Regex.IsMatch(DoGridCommand("get fan 1"), @"Fan 1: \d{1,5} RPM")){
                DoGridCommand("init");
            }

            DoGridCommand("set fans " + gpuFan + " speed " + gpuFanSpeed);
        }
        float cpuTemp = GetCPUTemp();
        Console.WriteLine("CPU temp: " + cpuTemp);
        float cpuPercentage = 100 * ((cpuTemp - cpuMinTemp) / (cpuMaxTemp - cpuMinTemp));
        Console.WriteLine("CPU percentage: " + cpuPercentage);
        float cpuFanSpeed = (cpuPercentage > maxSpeed ? maxSpeed : cpuPercentage < minSpeed ? minSpeed : cpuPercentage);
        cpuFanSpeed = (int) Math.Ceiling(cpuFanSpeed / minimumRound) * minimumRound;

        if(lastCPUSpeed != gpuFanSpeed){
            if(!Regex.IsMatch(DoGridCommand("get fan 1"), @"Fan 1: \d{1,5} RPM")){
                DoGridCommand("init");
            }

            DoGridCommand("set fans " + cpuFan + " speed " + cpuFanSpeed);
        }
        lastCPUSpeed = cpuFanSpeed;
        lastGPUSpeed = gpuFanSpeed;
    }

    private static string DoGridCommand(string command){
        ProcessStartInfo procStartInfo = new ProcessStartInfo("/bin/bash", "./gridfan " + command);
        procStartInfo.RedirectStandardOutput = true;
        procStartInfo.UseShellExecute = false;
        procStartInfo.CreateNoWindow = true;

        System.Diagnostics.Process proc = new System.Diagnostics.Process();
        proc.StartInfo = procStartInfo;
        proc.Start();
        string result = proc.StandardOutput.ReadToEnd();
        Console.WriteLine("Command result: " + result);
        System.Threading.Thread.Sleep(500);
        return result;
    }
}