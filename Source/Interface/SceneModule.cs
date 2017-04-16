﻿/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2015 Alexander Taylor
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using KSP.UI;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using FingerboxLib;
using KSPPluginFramework;

using System.Reflection;



namespace CrewQueue.Interface
{
    public abstract class SceneModule : MonoBehaviourExtended
    {
        public void CleanManifest()
        {
            if ( CrewAssignmentDialog.Instance != null)
            {
                VesselCrewManifest originalVesselManifest =  CrewAssignmentDialog.Instance.GetManifest();
                IList<PartCrewManifest> partCrewManifests = originalVesselManifest.GetCrewableParts();

                if (partCrewManifests != null && partCrewManifests.Count > 0)
                {
                    PartCrewManifest partManifest = partCrewManifests[0];
                    foreach (ProtoCrewMember crewMember in partManifest.GetPartCrew())
                    {
                        if (crewMember != null)
                        {
                            // Clean the root part
                            partManifest.RemoveCrewFromSeat(partManifest.GetCrewSeat(crewMember));
                        }
                    }
                    if (CrewQueueSettings.Instance.AssignCrews)
                    {
                        partManifest.AddCrewToOpenSeats(CrewQueue.Instance.GetCrewForPart(partManifest.PartInfo.partPrefab, new List<ProtoCrewMember>(), true));
                    }
                }

                 CrewAssignmentDialog.Instance.RefreshCrewLists(originalVesselManifest, true, true);
            }
        }

        UnityAction fillDelegate, defaultFillDelgate;

        public void RemapFillButton()
        {
            var buttonFillObj = CrewAssignmentDialog.Instance.transform.Find("VL/Buttons/Button Fill"); // <-- GetComponent<Button> on this if found
            if (buttonFillObj == null)
                return;

            Button buttonFillBtn = buttonFillObj.GetComponent<Button>();
           
            if (buttonFillBtn != null)
            {
                buttonFillBtn.onClick.RemoveAllListeners();                
                buttonFillBtn.onClick.SetPersistentListenerState(0, UnityEventCallState.Off);
                buttonFillBtn.onClick.AddListener(OnFillButton);
            }
        }

        public void OnFillButton()
        {
            VesselCrewManifest manifest =  CrewAssignmentDialog.Instance.GetManifest();

            Logging.Debug("Attempting to fill...");

            foreach (PartCrewManifest partManifest in manifest.GetCrewableParts())
            {
                Logging.Debug("Attempting to fill part - " + partManifest.PartInfo.name);
                bool vets = (partManifest == manifest.GetCrewableParts()[0]) ? true : false;
                partManifest.AddCrewToOpenSeats(CrewQueue.Instance.GetCrewForPart(partManifest.PartInfo.partPrefab, manifest.GetAllCrew(false), vets));
            }

            CrewAssignmentDialog.Instance.RefreshCrewLists(manifest, true, true);
            
        }

    }
}
