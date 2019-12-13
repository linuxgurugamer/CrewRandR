/*
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
using System.Linq;

using FingerboxLib;
using KSP.UI;
using UnityEngine;

namespace CrewRandR
{
    internal class CrewRandRRoster
    {
        // Singleton boilerplate
        private static CrewRandRRoster _Instance;
        public static CrewRandRRoster Instance
        {
            get
            {
                if (_Instance == null)
                {
                    Logging.Debug("Initializing Roster");
                    _Instance = new CrewRandRRoster();
                }

                return _Instance;
            }
        }

        // The basic idea here is going to be that the CrewRandR 'metadata' doesn't exist
        // until it is queried or set. When we query the data set, anything that doesn't
        // match an existing Kerbal is omitted, causing it to be deleted on the next on
        // the next save cycle.
        private HashSet<KerbalExtData> _ExtDataSet = new HashSet<KerbalExtData>();
        public IEnumerable<KerbalExtData> ExtDataSet
        {
            get
            {
                return _ExtDataSet.Where(x => x.EligibleForVacation);
            }
        }

        // These crew are the ones which are not available according to the current mod settings
        public IEnumerable<ProtoCrewMember> UnavailableCrew
        {
            get
            {
                return ExtDataSet.Where(k => k.OnVacation).Select(k => k.ProtoReference);
            }
        }

        // These crew are the ones which are available according to the current mod settings
        public IEnumerable<ProtoCrewMember> AvailableCrew
        {
            get
            {
                return HighLogic.CurrentGame.CrewRoster.Crew.Where(k => k.rosterStatus == ProtoCrewMember.RosterStatus.Available).Except(UnavailableCrew);                
            }
        }

        public IOrderedEnumerable<ProtoCrewMember> MostExperiencedCrew
        {
            get
            {
                return AvailableCrew.OrderByDescending(k => k.experienceLevel).ThenBy(k => k.GetLastMissionEndTime());
            }
        }

        public IOrderedEnumerable<ProtoCrewMember> LeastExperiencedCrew
        {
            get
            {
                return AvailableCrew.OrderBy(k => k.experienceLevel).ThenByDescending(k => k.GetLastMissionEndTime());
            }
        }

        public KerbalExtData GetExtForKerbal(string kerbal)
        {
            // Set only allows non-unique elements, so just try to add it anyway
            _ExtDataSet.Add(new KerbalExtData(kerbal));

            // Now find the element
            foreach (KerbalExtData data in _ExtDataSet)
            {
                if (data.Name == kerbal)
                {
                    return data;
                }
            }

            // This will never happen
            Logging.Error("Something dun broke, unsuccessful KerbalExtData lookup");
            return null;
        }

        public KerbalExtData GetExtForKerbal(ProtoCrewMember kerbal)
        {
            return GetExtForKerbal(kerbal.name);
        }

        public bool AddExtElement(KerbalExtData newElement)
        {
            return _ExtDataSet.Add(newElement);
        }

        public void Flush()
        {
            _ExtDataSet = new HashSet<KerbalExtData>();
        }

        public static void HideVacationingCrew()
        {
            Logging.Info("HideVacationCrew");
            foreach (ProtoCrewMember kerbal in CrewRandRRoster.Instance.UnavailableCrew.Where(k => k.rosterStatus == ProtoCrewMember.RosterStatus.Available))
            {
                kerbal.rosterStatus = CrewRandR.ROSTERSTATUS_VACATION;
            }

            if (CrewAssignmentDialog.Instance == null)
            {
                Logging.Info("CrewAssignmentDialog.Instance is null");
                return;
            }
            Logging.Info("CrewAssignmentDialog.Instance.RefreshCrewLists");
            CrewAssignmentDialog.Instance.RefreshCrewLists( CrewAssignmentDialog.Instance.GetManifest(), true, true);
        }

        public static void RestoreVacationingCrew()
        {
            Logging.Info("RestoreVacationingCrew");
            if (HighLogic.CurrentGame == null || HighLogic.CurrentGame.CrewRoster == null || HighLogic.CurrentGame.CrewRoster.Crew == null)
                return;
            
            var s = HighLogic.CurrentGame.CrewRoster.Crew;            

            foreach (ProtoCrewMember kerbal in HighLogic.CurrentGame.CrewRoster.Crew.Where(k => k.rosterStatus == CrewRandR.ROSTERSTATUS_VACATION))
            {
                Logging.Info("Crew on vacation: " + kerbal.nameWithGender);
                kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Available;
            }
            
            if (CrewAssignmentDialog.Instance == null)
            {
                Logging.Info("CrewAssignmentDialog.Instance is null");
                return;
            }
            Logging.Info("CrewAssignmentDialog.Instance.RefreshCrewLists");
            CrewAssignmentDialog.Instance.RefreshCrewLists( CrewAssignmentDialog.Instance.GetManifest(), true, true);
        }

        // Our storage node type.
        public class KerbalExtData
        {
            // This is the Kerbal which this ExtData is attached to.
            // Any mods which change the name of a Kerbal will need to update this value
            // TODO - add method to update values to API
            public string Name;

            // This will be true if the Kerbal has been sent on a mission while already on vacation.
            public bool ExtremelyFatigued = false;

            // Returns the start time of the current mission
            public double CurrentMissionStartTime = -1;

            // Returns the duration of the last mission
            public double LastMissionDuration = -1;
            public double LastMissionEndTime = -1;

            public double CumuledMissionDurationSinceLastRest = 0;

            public double VacationExpiry
            {
                get
                {
                    if (CrewRandRSettings.Instance != null && LastMissionDuration > -1 && LastMissionEndTime > -1)
                    {
                        double VacationScalar = CrewRandRSettings.Instance.VacationScalar;
                        double MinimumVacationDays = CrewRandRSettings.Instance.MinimumVacationDays * Utilities.GetDayLength;
                        double MaximumVacationDays = CrewRandRSettings.Instance.MaximumVacationDays * Utilities.GetDayLength;

                        double MinimumMissionTime = VacationScalar * MinimumVacationDays;

                        if (CumuledMissionDurationSinceLastRest < MinimumMissionTime)
                        {
                            return LastMissionEndTime;
                        }
                        else
                        {
                            double Expiry = LastMissionEndTime + (CumuledMissionDurationSinceLastRest * VacationScalar).Clamp(MinimumVacationDays, MaximumVacationDays);
                            return Expiry;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
            }

            public bool OnVacation
            {
                get
                {
                    return VacationExpiry > Planetarium.GetUniversalTime() ? true : false;
                }
            }

            public bool EligibleForVacation
            {
                get
                {
                    // Some kerbals (e.g. tourists) aren't considered crew, and
                    // should be exempt from all CrewRandR handling.
                    return ProtoReference != null;
                }
            }

            // Should return null if this is an unattached element
            public ProtoCrewMember ProtoReference
            {
                get
                {
                    return HighLogic.CurrentGame.CrewRoster.Crew.Where(k => k.name == Name).FirstOrDefault<ProtoCrewMember>();
                }
            }

            public ConfigNode ConfigNode
            {
                get
                {
                    ConfigNode _thisNode = new ConfigNode("KERBAL");

                    _thisNode.AddValue("Name", Name);
                    _thisNode.AddValue("CurrentMissionStartTime", CurrentMissionStartTime);
                    _thisNode.AddValue("GetLastMissionDuration", LastMissionDuration);
                    _thisNode.AddValue("LastMissionEndTime", LastMissionEndTime);
                    _thisNode.AddValue("ExtremelyFatigued", ExtremelyFatigued);
                    _thisNode.AddValue("CumuledMissionDurationSinceLastRest", CumuledMissionDurationSinceLastRest);

                    return _thisNode;
                }
            }

            public KerbalExtData(string newName)
            {
                Name = newName;
            }

            public KerbalExtData(ConfigNode configNode)
            {
                Name = configNode.GetValue("Name");
                LastMissionDuration = Convert.ToDouble(configNode.GetValue("GetLastMissionDuration"));
                LastMissionEndTime = Convert.ToDouble(configNode.GetValue("LastMissionEndTime"));
                ExtremelyFatigued = Convert.ToBoolean(configNode.GetValue("ExtremelyFatigued"));

                if (configNode.HasValue("CurrentMissionStartTime"))
                {
                    CurrentMissionStartTime = Convert.ToDouble(configNode.GetValue("CurrentMissionStartTime"));
                }
                else
                {
                    CurrentMissionStartTime = -1;  // Kerbal is not on a mission
                }

                if (configNode.HasValue("CumuledMissionDurationSinceLastRest"))
                {
                    CumuledMissionDurationSinceLastRest = Convert.ToDouble(configNode.GetValue("CumuledMissionDurationSinceLastRest"));
                }
                else
                {
                    CumuledMissionDurationSinceLastRest = -1;
                }
            }

            public override bool Equals(object obj)
            {
                if (obj != null && (obj as KerbalExtData) != null && (obj as KerbalExtData).Name == Name)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }

    public static class RosterExtensions
    {
        public static void SetOnMission(this ProtoCrewMember kerbal, double currentTime)
        {
            var kerbalExt = CrewRandRRoster.Instance.GetExtForKerbal(kerbal);

            // Ignore ineligible kerbals, to avoid logging bogus messages about
            // tourists starting missions.  (Since their data doesn't get saved,
            // they'd be forgotten and re-detected at every entry to the flight
            // scene.)
            if (!kerbalExt.EligibleForVacation) return;

            // This can be called multiple times during the mission, but we want
            // to know when the mission began, so only store the time if the
            // kerbal wasn't on a mission before now.
            if (kerbalExt.CurrentMissionStartTime <= -1)
            {
                Logging.Info("Started mission for " + kerbal.name + " at UT " + currentTime);

                kerbalExt.CurrentMissionStartTime = currentTime;
                kerbalExt.CumuledMissionDurationSinceLastRest = 0;
            }
        }

        public static void SetMissionFinished(this ProtoCrewMember kerbal, double currentTime)
        {
            var kerbalExt = CrewRandRRoster.Instance.GetExtForKerbal(kerbal);

            // Ignore ineligible kerbals, to avoid logging bogus messages about
            // tourists finishing missions.  (Since their data doesn't get
            // saved, they have no start time so they'd always get the zero
            // duration fallback, but they don't go on vacation anyway.)
            if (!kerbalExt.EligibleForVacation) return;

            Logging.Info("Finished mission for " + kerbal.name + " at UT " + currentTime);

            // If no mission-start time was tracked for this kerbal, use the
            // current time as a fallback, so that the duration is zero.  This
            // should only in rare situations (e.g. the vessel has been on
            // Kerbin since before this mod was installed, and was recovered
            // directly from the tracking station, without entering flight.)
            // The kerbal will get the minimum vacation time, which is
            // reasonble since there's no way to determine how long it should
            // really be.
            if (kerbalExt.CurrentMissionStartTime <= -1)
            {
                Logging.Debug("No mission start time recorded; using zero duation as fallback");

                kerbalExt.CurrentMissionStartTime = currentTime;
            }

            kerbalExt.LastMissionEndTime = currentTime;
            kerbalExt.LastMissionDuration = currentTime - kerbalExt.CurrentMissionStartTime;
            kerbalExt.CurrentMissionStartTime = -1;  // Mission has ended; "current" is now "last"
            kerbalExt.CumuledMissionDurationSinceLastRest += kerbalExt.LastMissionDuration;

            Logging.Debug("Mission duration was " + Utilities.GetFormattedTime(kerbalExt.LastMissionDuration));
        }

        public static double GetCurrentMissionStartTime(this ProtoCrewMember kerbal)
        {
            return CrewRandRRoster.Instance.GetExtForKerbal(kerbal).CurrentMissionStartTime;
        }

        public static double GetLastMissionDuration(this ProtoCrewMember kerbal)
        {
            return CrewRandRRoster.Instance.GetExtForKerbal(kerbal).LastMissionDuration;
        }

        public static double VacationExpiry(this ProtoCrewMember kerbal)
        {
            return CrewRandRRoster.Instance.GetExtForKerbal(kerbal).VacationExpiry;
        }

        public static bool IsOnVacation(this ProtoCrewMember kerbal)
        {
            return CrewRandRRoster.Instance.GetExtForKerbal(kerbal).OnVacation;
        }

        public static bool IsForcedVacation(this ProtoCrewMember kerbal)
        {
            return CrewRandRRoster.Instance.GetExtForKerbal(kerbal).ExtremelyFatigued;
        }

        public static double GetLastMissionEndTime(this ProtoCrewMember kerbal)
        {
            return CrewRandRRoster.Instance.GetExtForKerbal(kerbal).LastMissionEndTime;
        }
    }
}
