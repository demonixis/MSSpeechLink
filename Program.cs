// See https://aka.ms/new-console-template for more information
using MSSpeechLink;
using System.Globalization;

const int SleepIntervalMS = 500;
string ip = "127.0.0.1";
int port = 8831;
string lang = CultureInfo.CurrentCulture.Name;

foreach (string? arg in args)
{
    string[] tmp = arg.Split('=');
    if (tmp.Length != 2) continue;

    string key = tmp[0].ToLower().Trim();
    string value = tmp[1].ToLower().Trim();

    switch (key)
    {
        case "port": port = int.Parse(value); break;
        case "ip": ip = value; break;
        case "lang": lang = value; break;
    }
}

SpeechLink p = new(lang, ip, port);
bool running = true;

while (running)
{
    if (Console.Read() > 0)
        running = false;

    Thread.Sleep(SleepIntervalMS);
}