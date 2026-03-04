using System;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.Security.Cryptography;


namespace QLicense
{
 public   class HardwareInfo
    {
        /// <summary>
        /// Get volume serial number of drive C
        /// </summary>
        /// <returns></returns>
        private static string GetDiskVolumeSerialNumber()
        {
            try
            {
                using (ManagementObject _disk = new ManagementObject(@"Win32_LogicalDisk.deviceid=""c:"""))
                {
                    _disk.Get();
                    return _disk["VolumeSerialNumber"].ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetDiskVolumeSerialNumber failed: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Get CPU ID
        /// </summary>
        /// <returns></returns>
        private static string GetProcessorId()
        {
            try
            {
                using (ManagementObjectSearcher _mbs = new ManagementObjectSearcher("Select ProcessorId From Win32_processor"))
                using (ManagementObjectCollection _mbsList = _mbs.Get())
                {
                    string _id = string.Empty;
                    foreach (ManagementObject _mo in _mbsList)
                    {
                        _id = _mo["ProcessorId"].ToString();
                        break;
                    }
                    return _id;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetProcessorId failed: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Get motherboard serial number
        /// </summary>
        /// <returns></returns>
        public static string GetMotherboardID()
        {
            try
            {
                using (ManagementObjectSearcher _mbs = new ManagementObjectSearcher("Select SerialNumber From Win32_BaseBoard"))
                using (ManagementObjectCollection _mbsList = _mbs.Get())
                {
                    string _id = string.Empty;
                    foreach (ManagementObject _mo in _mbsList)
                    {
                        _id = _mo["SerialNumber"].ToString();
                        break;
                    }
                    return _id;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetMotherboardID failed: {ex.Message}");
                return string.Empty;
            }
        }

        private static IEnumerable<string> SplitInParts(string input, int partLength)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive.", "partLength");

            for (int i = 0; i < input.Length; i += partLength)
                yield return input.Substring(i, Math.Min(partLength, input.Length - i));
        }

        /// <summary>
        /// Combine CPU ID, Disk C Volume Serial Number and Motherboard Serial Number as device Id
        /// </summary>
        /// <returns></returns>
        public static string GenerateUID(string appName)
        {
            //Combine the IDs and get bytes
            string _id = string.Concat(appName, GetProcessorId(), GetMotherboardID());//, GetDiskVolumeSerialNumber());
            byte[] _byteIds = Encoding.UTF8.GetBytes(_id);

            //Use MD5 to get the fixed length checksum of the ID string
            byte[] _checksum;
            using (MD5CryptoServiceProvider _md5 = new MD5CryptoServiceProvider())
            {
                _checksum = _md5.ComputeHash(_byteIds);
            }

            //Convert checksum into 4 ulong parts and use BASE36 to encode both
            string _part1Id = BASE36.Encode(BitConverter.ToUInt32(_checksum, 0));
            string _part2Id = BASE36.Encode(BitConverter.ToUInt32(_checksum, 4));
            string _part3Id = BASE36.Encode(BitConverter.ToUInt32(_checksum, 8));
            string _part4Id = BASE36.Encode(BitConverter.ToUInt32(_checksum, 12));

            //Concat these 4 part into one string
            return string.Format("{0}-{1}-{2}-{3}", _part1Id, _part2Id, _part3Id, _part4Id);
        }

        public static byte[] GetUIDInBytes(string UID)
        {
            //Split 4 part Id into 4 ulong
            string[] _ids = UID.Split('-');

            if (_ids.Length != 4) throw new ArgumentException("Wrong UID");

            // Her part BASE36.Decode → ulong (8 byte), 4 part × 4 byte = 16 byte
            // BitConverter.GetBytes(uint) kullanarak 4 byte per part = toplam 16 byte
            byte[] _value = new byte[16];
            Buffer.BlockCopy(BitConverter.GetBytes((uint)BASE36.Decode(_ids[0])), 0, _value, 0,  4);
            Buffer.BlockCopy(BitConverter.GetBytes((uint)BASE36.Decode(_ids[1])), 0, _value, 4,  4);
            Buffer.BlockCopy(BitConverter.GetBytes((uint)BASE36.Decode(_ids[2])), 0, _value, 8,  4);
            Buffer.BlockCopy(BitConverter.GetBytes((uint)BASE36.Decode(_ids[3])), 0, _value, 12, 4);

            return _value;            
        }

        public static bool ValidateUIDFormat(string UID)
        {
            if (!string.IsNullOrWhiteSpace(UID))
            {
                string[] _ids = UID.Split('-');

                return (_ids.Length == 4);
            }
            else
            {
                return false;
            }

        }
    }
}
