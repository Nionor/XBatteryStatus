﻿using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace XBatteryStatus
{
    public class MyApplicationContext : ApplicationContext
    {
        NotifyIcon notifyIcon = new NotifyIcon();

        private Timer timer1;

        public BluetoothLEDevice pairedGamepad;
        public GattCharacteristic batteryCharacteristic;

        private int lastBattery = 100;

        public MyApplicationContext()
        {
            notifyIcon.Icon = Properties.Resources.iconQ;
            notifyIcon.Text = "XBatteryStatus: Looking for paired controller";
            notifyIcon.Visible = true;

            FindBleController();

            timer1 = new Timer();
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = 10000;
            timer1.Start();
        }

        async private void FindBleController()
        {
            foreach (var device in await DeviceInformation.FindAllAsync())
            {
                try
                {
                    BluetoothLEDevice bleDevice = await BluetoothLEDevice.FromIdAsync(device.Id);

                    if (bleDevice != null && bleDevice.Appearance.SubCategory == BluetoothLEAppearanceSubcategories.Gamepad)//get the gamepads
                    {
                        GattDeviceService service = bleDevice.GetGattService(new Guid("0000180f-0000-1000-8000-00805f9b34fb"));
                        GattCharacteristic characteristic = service.GetCharacteristics(new Guid("00002a19-0000-1000-8000-00805f9b34fb")).First();

                        if (service != null && characteristic != null)//get the gamepads with battery status
                        {
                            pairedGamepad = bleDevice;//use the first one
                            batteryCharacteristic = characteristic;
                            notifyIcon.Visible = false;
                            bleDevice.ConnectionStatusChanged += ConnectionStatusChanged;
                            Update();
                            return;
                        }
                    }
                }
                catch { }
            }

            if (batteryCharacteristic == null)
            {
                notifyIcon.Icon = Properties.Resources.iconE;
                notifyIcon.Text = "XBatteryStatus: No paired BLE controller with battery status found";
            }
        }

        private async void ReadBattery()
        {
            if (pairedGamepad != null && batteryCharacteristic != null)
            {
                if (pairedGamepad.ConnectionStatus == BluetoothConnectionStatus.Connected)
                {
                    GattReadResult result = await batteryCharacteristic.ReadValueAsync();

                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        var reader = DataReader.FromBuffer(result.Value);
                        int val = reader.ReadByte();
                        string notify = val.ToString() + "% - " + pairedGamepad.Name;
                        notifyIcon.Text = "XBatteryStatus: " + notify;
                        if (val < 5) notifyIcon.Icon = Properties.Resources.icon00;
                        else if (val < 15) notifyIcon.Icon = Properties.Resources.icon10;
                        else if (val < 25) notifyIcon.Icon = Properties.Resources.icon20;
                        else if (val < 35) notifyIcon.Icon = Properties.Resources.icon30;
                        else if (val < 45) notifyIcon.Icon = Properties.Resources.icon40;
                        else if (val < 55) notifyIcon.Icon = Properties.Resources.icon50;
                        else if (val < 65) notifyIcon.Icon = Properties.Resources.icon60;
                        else if (val < 75) notifyIcon.Icon = Properties.Resources.icon70;
                        else if (val < 85) notifyIcon.Icon = Properties.Resources.icon80;
                        else if (val < 95) notifyIcon.Icon = Properties.Resources.icon90;
                        else notifyIcon.Icon = Properties.Resources.icon100;

                        if ((lastBattery > 15 && val <= 15) || (lastBattery > 10 && val <= 10) || (lastBattery > 5 && val <= 5))
                        {
                            new ToastContentBuilder().AddText("Low Battery").AddText(notify)
                                .Show();
                        }
                        lastBattery = val;
                    }
                }
            }
        }

        private void ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            Update();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ReadBattery();
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
        }

        public void Update()
        {
            notifyIcon.Visible = pairedGamepad.ConnectionStatus == BluetoothConnectionStatus.Connected;
            ReadBattery();
        }
    }
}