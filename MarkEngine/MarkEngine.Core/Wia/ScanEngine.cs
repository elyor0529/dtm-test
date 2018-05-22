﻿/* 
 * Optical Mark Recognition 
 * Copyright 2015, Justin Fyfe
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * Author: Justin
 * Date: 4-16-2015
 */

using System;
using System.Collections.Generic;
using System.Threading;
using WIA;

namespace OmrMarkEngine.Wia
{
    /// <summary>
    ///     Represents an image acquisition engine
    /// </summary>
    public class ScanEngine
    {
        // Thread
        private Thread m_scanThread;

        /// <summary>
        ///     When true, single images only
        /// </summary>
        public bool SingleOnly { get; set; }

        /// <summary>
        ///     A scan has been acquired from the image source
        /// </summary>
        public event EventHandler<ScanCompletedEventArgs> ScanCompleted;

        /// <summary>
        ///     Start scan
        /// </summary>
        public void ScanAsync(ScannerInfo source)
        {
            if (source == null)
                return;

            var wiaDevice = source.GetDevice();
            // Manager
            var wiaManager = new DeviceManager();

            var hasMorePages = true;
            while (hasMorePages)
                try
                {
                    // Get items
                    var wiaItem = wiaDevice.Items[1];
                    int inColor = 2, dpi = 300;
                    wiaItem.Properties["6146"].set_Value(inColor);
                        //Item MUST be stored in a variable THEN the properties must be set.
                    wiaItem.Properties["6147"].set_Value(dpi);
                    wiaItem.Properties["6148"].set_Value(dpi);

//                        var imageFile = (ImageFile)(new CommonDialog()).ShowTransfer(wiaItem, "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}", false); //wiaItem.Transfer("{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}") as WIA.ImageFile;

                    var imageFile = wiaItem.Transfer("{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}") as ImageFile;

                    if (ScanCompleted != null)
                        ScanCompleted(this, new ScanCompletedEventArgs(imageFile.FileData.get_BinaryData()));
                }
                catch (Exception)
                {
                    break;
                }
                finally
                {
                    //determine if there are any more pages waiting
                    Property documentHandlingSelect = null;
                    Property documentHandlingStatus = null;

                    foreach (Property prop in wiaDevice.Properties)
                    {
                        if (prop.PropertyID == WIA_PROPERTIES.WIA_DPS_DOCUMENT_HANDLING_SELECT)
                            documentHandlingSelect = prop;
                        if (prop.PropertyID == WIA_PROPERTIES.WIA_DPS_DOCUMENT_HANDLING_STATUS)
                            documentHandlingStatus = prop;
                    }
                    hasMorePages = false; //assume there are no more pages
                    if (documentHandlingSelect != null)
                        //may not exist on flatbed scanner but required for feeder
                        if ((Convert.ToUInt32(documentHandlingSelect.get_Value()) &
                             WIA_DPS_DOCUMENT_HANDLING_SELECT.FEEDER) != 0)
                            hasMorePages = (Convert.ToUInt32(documentHandlingStatus.get_Value()) &
                                            WIA_DPS_DOCUMENT_HANDLING_STATUS.FEED_READY) != 0;

                    if (hasMorePages && SingleOnly)
                        hasMorePages = false;
                }
        }

        /// <summary>
        ///     Get a list of devices
        /// </summary>
        public List<ScannerInfo> GetWiaDevices()
        {
            var mgr = new DeviceManager();
            var retVal = new List<ScannerInfo>();

            foreach (DeviceInfo info in mgr.DeviceInfos)
                if (info.Type == WiaDeviceType.ScannerDeviceType)
                    foreach (Property p in info.Properties)
                        if (p.Name == "Name")
                            retVal.Add(new ScannerInfo(p.get_Value().ToString(), info.DeviceID));
            return retVal;
        }

        /// <summary>
        ///     Scan a single image
        /// </summary>
        public byte[] ScanSingle(ScannerInfo source)
        {
            var wiaDevice = source.GetDevice();
            // Manager
            var wiaManager = new DeviceManager();

            try
            {
                // Get items
                var wiaItem = wiaDevice.Items[1];
                int inColor = 2, dpi = 300;
                wiaItem.Properties["6146"].set_Value(inColor);
                    //Item MUST be stored in a variable THEN the properties must be set.
                wiaItem.Properties["6147"].set_Value(dpi);
                wiaItem.Properties["6148"].set_Value(dpi);

                var imageFile =
                        (ImageFile)
                        new CommonDialog().ShowTransfer(wiaItem, "{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}", false);
                    //wiaItem.Transfer("{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}") as WIA.ImageFile;

                return imageFile.FileData.get_BinaryData();
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}