using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MPPC.App
{
   public class SerialLogger 
    {
        private bool isListening = false;
        public CancellationTokenSource CancellationTokenSource { get; set; }

        public void BeginListen(SerialPort port, Action<string> onMessage)
        {
            if (isListening)
                return;
            try
            {
                isListening = true;
                   CancellationTokenSource = new CancellationTokenSource();
                Task.Run(async () =>
                {
                    while (!CancellationTokenSource.IsCancellationRequested)
                    {
                        string message = port.ReadExisting();
                        if (!string.IsNullOrEmpty(message) && message.Length > 2)
                        {
                            onMessage(message);
                        }

                        await Task.Delay(500);
                    }
                }, CancellationTokenSource.Token);
            }
            catch (TaskCanceledException) { }
        }

        public void StopListen()
        {
            isListening = false;
            CancellationTokenSource.Cancel();
        }
    }
}
