using System;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        string virtualMaster = File.ReadAllText("VIR_MASTER.txt");
        string ScannerPortName1 = File.ReadAllText("SCANNER1.txt");
        string ScannerPortName2 = File.ReadAllText("SCANNER2.txt");

        using (SerialPort _virtualMaster = new SerialPort(virtualMaster))
        using (SerialPort _ScannerPortName1 = new SerialPort(ScannerPortName1))
        using (SerialPort _ScannerPortName2 = new SerialPort(ScannerPortName2))
        {
            SetupPort(_virtualMaster);
            SetupPort(_ScannerPortName1);
            SetupPort(_ScannerPortName2);

            try
            {
                _virtualMaster.Open();
                _ScannerPortName1.Open();
                _ScannerPortName2.Open();

                Console.WriteLine("Aplikacja jest gotowa do wysyłania danych.");

                while (true)
                {
                    try
                    {
                        if (_ScannerPortName1.BytesToRead > 0 && _ScannerPortName2.BytesToRead > 0)
                        {
                            byte[] buffer1 = new byte[_ScannerPortName1.BytesToRead];
                            int bytesRead1 = await _ScannerPortName1.BaseStream.ReadAsync(buffer1, 0, buffer1.Length);
                            byte[] buffer2 = new byte[_ScannerPortName2.BytesToRead];
                            int bytesRead2 = await _ScannerPortName2.BaseStream.ReadAsync(buffer2, 0, buffer2.Length);
                            string ScannerResult1 = Encoding.ASCII.GetString(buffer1);
                            Console.WriteLine("Dane z ScannerPortName1: " + ScannerResult1);
                            string ScannerResult2 = Encoding.ASCII.GetString(buffer2);
                            Console.WriteLine("Dane z ScannerPortName2: " + ScannerResult2);

                            if (ZawieraNOREAD(ScannerResult1) && ZawieraNOREAD(ScannerResult2))
                            {
                                Console.WriteLine("Wysyłam NOREAD na virtual master.");
                                WriteDataToVirtualMaster(_virtualMaster, buffer1, bytesRead1);
                            }
                            else
                            {
                                if (ZawieraNOREAD(ScannerResult1) == false)
                                {
                                    Console.WriteLine("wysyłam scanner 1 result na virtual master");
                                    WriteDataToVirtualMaster(_virtualMaster, buffer1, bytesRead1);
                                }
                                else if (ZawieraNOREAD(ScannerResult2) == false)
                                {
                                    Console.WriteLine("wysyłam scanner 2 result na virtual master");
                                    WriteDataToVirtualMaster(_virtualMaster, buffer2, bytesRead2);
                                }
                            }



                            bool PorownajLancuchy(string lancuch1, string lancuch2)
                            // Porównanie bez uwzględniania wielkości liter
                            {
                                return string.Equals(lancuch1, lancuch2, StringComparison.OrdinalIgnoreCase);
                            }

                            bool ZawieraNOREAD(string lancuch)
                            {
                                // Sprawdzenie, czy łańcuch zawiera frazę "NOREAD" (bez uwzględniania wielkości liter)
                                return lancuch.IndexOf("NOREAD", StringComparison.OrdinalIgnoreCase) >= 0;
                            }


                            await Task.Delay(TimeSpan.FromMilliseconds(100)); // Opóźnienie pętli
                        }
                    }
                    catch (Exception ex)
                    {
                        // Obsługa wyjątku w przypadku zamknięcia portu lub innych problemów
                        Console.WriteLine("Wystąpił błąd: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Wystąpił błąd: " + ex.Message);
            }
            finally
            {
                ClosePort(_virtualMaster);
                ClosePort(_ScannerPortName1);
                ClosePort(_ScannerPortName2);

                Console.WriteLine("Aplikacja została zatrzymana.");
                Console.ReadLine();
            }
        }
    }

    static void WriteDataToVirtualMaster(SerialPort virtualMaster, byte[] data, int length)
    {
        if (virtualMaster.BytesToWrite == 0)
        {
            Task sendToVirtualPort = virtualMaster.BaseStream.WriteAsync(data, 0, length);
            Task delayTask = Task.Delay(TimeSpan.FromSeconds(2)); // Ograniczenie czasu oczekiwania

            Task completedTask = Task.WhenAny(sendToVirtualPort, delayTask).Result;

            if (completedTask == sendToVirtualPort)
            {
                sendToVirtualPort.Wait(); // Poczekaj, aż przesyłanie zostanie zakończone
            }
            else
            {
                Console.WriteLine("Ograniczenie czasowe przekroczone dla portu wirtualnego.");
                // Możesz podjąć odpowiednie działania, np. zakończenie lub ponowne próby
            }
        }
    }

    static string ReadPortNameFromFile(string fileName)
    {
        return File.ReadAllText(fileName);
    }

    static void SetupPort(SerialPort port)
    {
        port.BaudRate = 9600;
        // Ustaw inne parametry portu, jeśli to konieczne
    }

    static void ClosePort(SerialPort port)
    {
        if (port.IsOpen)
            port.Close();
    }
}
