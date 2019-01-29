using System;
using System.Text;
using Crestron.SimplSharp;                     
using Crestron.SimplSharpPro;                  
using Crestron.SimplSharpPro.Diagnostics;	
using Crestron.SimplSharpPro.DeviceSupport;    
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharp.CrestronXmlLinq;
using Crestron.SimplSharp.CrestronIO;                 


public class Schedule 
{
    private BasicTriList TP;
    private DateTime datetime = new DateTime();
    private CTimer checker;

    private const uint startTimeText=1201;  
    private const uint stopTimeText=1202;
    
    private const uint startMon = 1211;
    private const uint startTue = 1212;
    private const uint startWed = 1213;
    private const uint startThu = 1214;
    private const uint startFri = 1215;
    private const uint startSat = 1216;
    private const uint startSun = 1217;
    private uint[] startDays = { startMon, startTue, startWed, startThu, startFri, startSat, startSun };

    private const uint stopMon = 1221;
    private const uint stopTue = 1222;
    private const uint stopWed = 1223;
    private const uint stopThu = 1224;
    private const uint stopFri = 1225;
    private const uint stopSat = 1226;
    private const uint stopSun = 1227;
    private uint[] stopDays = { stopMon, stopTue, stopWed, stopThu, stopFri, stopSat, stopSun };
        
    private const uint startUpMin = 1231;
    private const uint startDownMin = 1232;
    private const uint startUpHr = 1233;
    private const uint startDownHr = 1234;
    private const uint stopUpMin = 1235;
    private const uint stopDownMin = 1236;
    private const uint stopUpHr = 1237;
    private const uint stopDownHr = 1238;

    private volatile int startHour=0;
    private volatile int stopHour=0;
    private volatile int startMin=0;
    private volatile int stopMin=0;

    private volatile uint holdButton;
    private CTimer HeldEvent;
    private volatile uint buttonHoldTime;

    private volatile string FileName;

    public event EventHandler AutoStart;
    public event EventHandler AutoStop;

    private bool debug = true; 
    
    
    /// <summary>
    /// Initialise class with touch panel device  
    /// </summary>
    /// <param name="device"></param>
    public void Init(BasicTriList device, string filename)
    {
        TP = device;
        FileName = filename;
        TP.SigChange += new SigEventHandler(TPSigChangeHandler);
        ReadDataFromXMLFile();
        StartChecker();
        HeldEvent = new CTimer(HeldEventCallback, Timeout.Infinite);
        buttonHoldTime = 200;
    }    
    

    /// <summary>
    /// Method to enable/disable debug messages
    /// </summary>
    /// <param name="state"></param>
    public void DebugEnable(bool state)
    {
        debug = state;
        CrestronConsole.PrintLine("[SCHEDULE] debugging set to {0}", state);        
    }   


    private void Debug(string msg)
    {
        if (debug)
        {
            CrestronConsole.PrintLine("[SCHEDULE] {0}", msg);
        }
    }


    private void StartChecker()
    {
        checker = new CTimer(CheckerCallBack,null,0,60000);
        Debug("checker started");
    }


    private void CheckerCallBack(object notused)
    {
        datetime = DateTime.Now;
        int dayw = (int)datetime.DayOfWeek;

        int hour = datetime.Hour;
        int min = datetime.Minute;

        // calculate start day button id                       
        uint btnStartDay = (uint)dayw + 1210;
        if (btnStartDay == 1210) 
			btnStartDay = startSun;
        
        if (TP.BooleanInput[btnStartDay].BoolValue == true)
        {                       
            if (hour == startHour && min == startMin)
            {
                Debug("Auto-start has been triggered!!!");
                AutoStartTriggered(null);
                return; 
            }
        }
                
        // calculate stop day button id
        uint btnStopDay = (uint)dayw + 1220;
        if (btnStopDay == 1220)
            btnStopDay = stopSun;

        if (TP.BooleanInput[btnStopDay].BoolValue == true)
        {         
            if (hour == stopHour && min == stopMin)
            {
                Debug("Auto-shutdown has been triggered!!!");
                AutoShutdownTriggered(null);
            }
        }
    }


    // fire callback functions
    protected virtual void AutoStartTriggered(EventArgs e)
    {
        EventHandler eh = AutoStart;

        if (eh != null)
            eh(this, e);
    }


    protected virtual void AutoShutdownTriggered(EventArgs e)
    {
        EventHandler eh = AutoStop;

        if (eh != null)
            eh(this, e);
    }
            
      
    private void UpdateUI()
    {
        try
        {
            if (startMin < 10)
            {
                string startTime = String.Format("{0}:0{1}", startHour, startMin);
                TP.StringInput[startTimeText].StringValue = startTime;
            }
            else
            {
                string startTime = String.Format("{0}:{1}", startHour, startMin);
                TP.StringInput[startTimeText].StringValue = startTime;
            }

            if (stopMin < 10)
            {
                string stopTime = String.Format("{0}:0{1}", stopHour, stopMin);
                TP.StringInput[stopTimeText].StringValue = stopTime;
            }
            else
            {
                string stopTime = String.Format("{0}:{1}", stopHour, stopMin);
                TP.StringInput[stopTimeText].StringValue = stopTime;
            }
        }
        catch (Exception ex)
        {
            Debug(String.Format("UpdateStartStopTimeText error: {0}", ex));
        }
    }
    

    private void HeldEventCallback(object notUsed)
    {
        switch (holdButton)
        {
            case startUpMin:
                {
                    if (startMin < 59)
                        startMin++;
                    else
                        startMin = 0;
                } break;
            case startDownMin:
                {
                    if (startMin > 0)
                        startMin--;
                    else
                        startMin = 59;
                } break;
            case startUpHr:
                {
                    if (startHour < 23)
                        startHour++;
                    else
                        startHour = 0;
                } break;
            case startDownHr:
                {
                    if (startHour > 0)
                        startHour--;
                    else
                        startHour = 23;
                } break;

            case stopUpMin:
                {
                    if (stopMin < 59)
                        stopMin++;
                    else
                        stopMin = 0;
                } break;
            case stopDownMin:
                {
                    if (stopMin > 0)
                        stopMin--;
                    else
                        stopMin = 59;
                } break;
            case stopUpHr:
                {
                    if (stopHour < 23)
                        stopHour++;
                    else
                        stopHour = 0;

                } break;
            case stopDownHr:
                {
                    if (stopHour > 0)
                        stopHour--;
                    else
                        stopHour = 23;
                } break;
        }

        SaveDataToAnXmlFile(string.Format("{0}\\{1}.xml", Directory.GetApplicationDirectory(), FileName));
        HeldEvent.Reset(buttonHoldTime, Timeout.Infinite);
    }


    private void TPSigChangeHandler(BasicTriList TP, SigEventArgs args)
    {
        try
        {
            if (args.Sig.Type == eSigType.Bool && args.Sig.BoolValue)
            {
                if (args.Sig.Number >= startMon && args.Sig.Number <= startSun)
                {
                    TP.BooleanInput[args.Sig.Number].BoolValue = !TP.BooleanInput[args.Sig.Number].BoolValue;
                    SaveDataToAnXmlFile(string.Format("{0}\\{1}.xml", Directory.GetApplicationDirectory(), FileName));

                    return;
                }

                if (args.Sig.Number >= stopMon && args.Sig.Number <= stopSun)
                {
                    TP.BooleanInput[args.Sig.Number].BoolValue = !TP.BooleanInput[args.Sig.Number].BoolValue;
                    SaveDataToAnXmlFile(string.Format("{0}\\{1}.xml", Directory.GetApplicationDirectory(), FileName));

                    return;
                }

                if (args.Sig.Number >= startUpMin && args.Sig.Number <= stopDownHr)
                {
                    holdButton = args.Sig.Number;
                    HeldEventCallback("");
                    HeldEvent.Reset(buttonHoldTime, Timeout.Infinite);
                }
            }
            else
            {
                if (args.Sig.Number >= startUpMin && args.Sig.Number <= stopDownHr)
                {
                    HeldEvent.Stop();
                }
            }
        }
        catch (Exception ex)
        {
            Debug(String.Format("Error in TPSigChangeHandler: {0}", ex));
        }
    }



    #region read/write config



    XDocument GetScheduleData()
    {
        XDocument config = new XDocument(
        new XDeclaration("1.0", "utf-8", "yes"),
        new XComment("Scheduler Configuration"),
        new XElement("configdata",            
            new XElement("startup",
                new XElement("startHour",String.Format("{0}", startHour)),
                new XElement("startMin", String.Format("{0}", startMin)),
                new XElement("startMon", String.Format("{0}", TP.BooleanInput[startMon].BoolValue)),
                new XElement("startTue", String.Format("{0}", TP.BooleanInput[startTue].BoolValue)),
                new XElement("startWed", String.Format("{0}", TP.BooleanInput[startWed].BoolValue)),
                new XElement("startThu", String.Format("{0}", TP.BooleanInput[startThu].BoolValue)),
                new XElement("startFri", String.Format("{0}", TP.BooleanInput[startFri].BoolValue)),
                new XElement("startSat", String.Format("{0}", TP.BooleanInput[startSat].BoolValue)),
                new XElement("startSun", String.Format("{0}", TP.BooleanInput[startSun].BoolValue))),                      
            new XElement("shutdown",
                new XElement("stopHour",String.Format("{0}", stopHour)),
                new XElement("stopMin", String.Format("{0}", stopMin)),
                new XElement("stopMon", String.Format("{0}", TP.BooleanInput[stopMon].BoolValue)),
                new XElement("stopTue", String.Format("{0}", TP.BooleanInput[stopTue].BoolValue)),
                new XElement("stopWed", String.Format("{0}", TP.BooleanInput[stopWed].BoolValue)),
                new XElement("stopThu", String.Format("{0}", TP.BooleanInput[stopThu].BoolValue)),
                new XElement("stopFri", String.Format("{0}", TP.BooleanInput[stopFri].BoolValue)),
                new XElement("stopSat", String.Format("{0}", TP.BooleanInput[stopSat].BoolValue)),
                new XElement("stopSun", String.Format("{0}", TP.BooleanInput[stopSun].BoolValue)))
                ));
        return config;
    }

    
    void SaveDataToAnXmlFile(string filename)
    {
        try
        {
            XDocument config = GetScheduleData(); // build data
            config.Save(filename); // save data to file
            ReadDataFromXMLFile(); // reload it
        }

        catch (Exception ex)
        {
            Debug(String.Format("Error in SaveDataToAnXmlFile: {0}", ex.Message));
        }
    }


    void ReadDataFromXMLFile()
    {
        try
        {
            XNamespace xns = "";

            string schedulerFilePath = string.Format("{0}\\{1}.xml", Directory.GetApplicationDirectory(),FileName);

            if (File.Exists(schedulerFilePath))
            {
                Debug("Schedule xml file found");

                XmlReader reader = new XmlTextReader(String.Format("{0}\\{1}.xml", Directory.GetApplicationDirectory(),FileName));
                XDocument retrievedData = XDocument.Load(reader);
                reader.Close();

                var startupData = retrievedData.Descendants(xns + "startup");
                foreach (XElement item in startupData)
                {
                    startHour = Convert.ToUInt16(item.Element(xns + "startHour").Value);
                    startMin = Convert.ToUInt16(item.Element(xns + "startMin").Value);
                    TP.BooleanInput[startMon].BoolValue= Convert.ToBoolean(item.Element(xns + "startMon").Value);
                    TP.BooleanInput[startTue].BoolValue= Convert.ToBoolean(item.Element(xns + "startTue").Value);
                    TP.BooleanInput[startWed].BoolValue= Convert.ToBoolean(item.Element(xns + "startWed").Value);
                    TP.BooleanInput[startThu].BoolValue= Convert.ToBoolean(item.Element(xns + "startThu").Value);
                    TP.BooleanInput[startFri].BoolValue= Convert.ToBoolean(item.Element(xns + "startFri").Value);
                    TP.BooleanInput[startSat].BoolValue= Convert.ToBoolean(item.Element(xns + "startSat").Value);
                    TP.BooleanInput[startSun].BoolValue= Convert.ToBoolean(item.Element(xns + "startSun").Value);
                }

                var shutdownData = retrievedData.Descendants(xns + "shutdown");
                foreach (XElement item in shutdownData)
                {
                    stopHour = Convert.ToUInt16(item.Element(xns + "stopHour").Value);
                    stopMin = Convert.ToUInt16(item.Element(xns + "stopMin").Value);
                    TP.BooleanInput[stopMon].BoolValue = Convert.ToBoolean(item.Element(xns + "stopMon").Value);
                    TP.BooleanInput[stopTue].BoolValue = Convert.ToBoolean(item.Element(xns + "stopTue").Value);
                    TP.BooleanInput[stopWed].BoolValue = Convert.ToBoolean(item.Element(xns + "stopWed").Value);
                    TP.BooleanInput[stopThu].BoolValue = Convert.ToBoolean(item.Element(xns + "stopThu").Value);
                    TP.BooleanInput[stopFri].BoolValue = Convert.ToBoolean(item.Element(xns + "stopFri").Value);
                    TP.BooleanInput[stopSat].BoolValue = Convert.ToBoolean(item.Element(xns + "stopSat").Value);
                    TP.BooleanInput[stopSun].BoolValue = Convert.ToBoolean(item.Element(xns + "stopSun").Value);
                }

                UpdateUI();
            }
            else
            {
                Debug("Could not find scheduler xml file");
            }
        }

        catch (Exception ex)
        {
            Debug(String.Format("Error in ReadDataFromXMLFile: {0}", ex.Message));
        }


    }

    


    #endregion  

}
        

