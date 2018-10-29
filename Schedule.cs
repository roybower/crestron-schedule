using System;
using System.Text;
using Crestron.SimplSharp;                      // For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                   // For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.Diagnostics;		// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;     // For Generic Device Support


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

    public event EventHandler AutoStart;
    public event EventHandler AutoStop;

    private bool debug = true; 
    
    
    /// <summary>
    /// Initialise class with touch panel device  
    /// </summary>
    /// <param name="device"></param>
    public void Init(BasicTriList device)
    {
        TP = device;
        TP.SigChange += new SigEventHandler(TPSigChangeHandler);     
        UpdateStartStopTimeText();
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
            
      
    private void UpdateStartStopTimeText()
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

        UpdateStartStopTimeText();
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
                    int Day = (int)args.Sig.Number - 1210;
                    TP.BooleanInput[args.Sig.Number].BoolValue = !TP.BooleanInput[args.Sig.Number].BoolValue;

                    return;
                }

                if (args.Sig.Number >= stopMon && args.Sig.Number <= stopSun)
                {
                    int Day = (int)args.Sig.Number - 1220;
                    TP.BooleanInput[args.Sig.Number].BoolValue = !TP.BooleanInput[args.Sig.Number].BoolValue;

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

}
        

