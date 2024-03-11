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
using System.Collections;
using System.Reflection;



namespace CrewRandR
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION })]
    class CrewRandRSettings : ScenarioModule
    {
        // Singleton boilerplate
        private static CrewRandRSettings _Instance;
        internal static CrewRandRSettings Instance
        {

            get
            {
#if false
                if (_Instance == null)
                {
                    throw new Exception("ERROR: Attempted to query CrewRandR.Data before it was loaded.");
                }
#endif
                return _Instance;
            }
        }

#if false
        [KSPField(isPersistant = true)]
        public bool HideSettingsIcon = false;
#endif

#if false
        [KSPField(isPersistant = true)]
        public bool AssignCrews = true;

        [KSPField(isPersistant = true)]
        public double VacationScalar = 0.1;

        [KSPField(isPersistant = true)]
        public int MinimumVacationDays = 7;

        [KSPField(isPersistant = true)]
        public int MaximumVacationDays = 28;
#endif
        public bool AssignCrews {  get { return HighLogic.CurrentGame.Parameters.CustomParams<CrewRandRCustomParams>().AssignCrews; } }
        public bool AssignAllCrews { get { return HighLogic.CurrentGame.Parameters.CustomParams<CrewRandRCustomParams>().AssignAllCrews; } }
        public double VacationScalar { get { return HighLogic.CurrentGame.Parameters.CustomParams<CrewRandRCustomParams>().vacationScalar; } }
        public int MinimumVacationDays { get { return HighLogic.CurrentGame.Parameters.CustomParams<CrewRandRCustomParams>().MinimumVacationDays; } }
        public int MaximumVacationDays { get { return HighLogic.CurrentGame.Parameters.CustomParams<CrewRandRCustomParams>().MaximumVacationDays; } }


        public int MinimumVacationHours { get { return HighLogic.CurrentGame.Parameters.CustomParams<CrewRandRCustomParams>().MinimumVacationHours; } }
        public int MaximumVacationHours { get { return HighLogic.CurrentGame.Parameters.CustomParams<CrewRandRCustomParams>().MaximumVacationHours; } }

        public double ShortMissionMaxLength { get { return HighLogic.CurrentGame.Parameters.CustomParams<CrewRandRCustomParams>().ShortMissionMaxLength; } }


        public bool Enabled { get { return HighLogic.CurrentGame.Parameters.CustomParams<CrewRandRCustomParams>().enabled; } }

        public override void OnAwake()
        {
            _Instance = this;

            GameEvents.onFlightReady.Add(onFlightReady);
        }

        void OnDestroy()
        {
            _Instance = null;

            GameEvents.onFlightReady.Remove(onFlightReady);
        }

        // ScenarioModule methods
        public override void OnLoad(ConfigNode rootNode)
        {
            CrewRandRRoster.Instance.Flush();
            if (rootNode.HasNode("CrewList"))
            {
                rootNode = rootNode.GetNode("CrewList");
                IEnumerable<ConfigNode> crewNodes = rootNode.GetNodes();

                foreach (ConfigNode crewNode in crewNodes)
                {
                    CrewRandRRoster.Instance.AddExtElement(new CrewRandRRoster.KerbalExtData(crewNode));
                }
            }
        }

        public override void OnSave(ConfigNode rootNode)
        {
            rootNode.RemoveNode("CrewList");
            ConfigNode crewNodes = new ConfigNode("CrewList");

            foreach (CrewRandRRoster.KerbalExtData crewNode in CrewRandRRoster.Instance.ExtDataSet)
            {
                bool rosterHidden = (crewNode.ProtoReference.rosterStatus == CrewRandR.ROSTERSTATUS_VACATION);

                if (rosterHidden)
                {
                    crewNode.ProtoReference.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                }

                crewNodes.AddNode(crewNode.ConfigNode);

                if (rosterHidden)
                {
                    crewNode.ProtoReference.rosterStatus = CrewRandR.ROSTERSTATUS_VACATION;
                }
            }

            rootNode.AddNode(crewNodes);
        }

        private void onFlightReady()
        {
            // All kerbals who are in vessels are on missions.

            // In general we're just interested in the crew of a new vessel
            // being placed on a launchpad, since every mission starts there,
            // but there doesn't seem to be an event that actually provides the
            // crew of such a vessel.  (OnVesselRollout is ideal in principle,
            // but it doesn't provide a crew list.  OnVesselCreate seems
            // promising, but a new vessel's crew list is empty when that fires.
            // Other OnVessel* events have similar problems.)  So instead, we
            // just look at *all* the vessels when they're all ready.  This
            // isn't too costly since it only happens once, and it ensures that
            // nothing gets missed.

            // In particular, this also finds crew in vessels that were launched
            // before this mod was installed (or using an old version that
            // didn't track kerbals' mission time in this way).  In those cases,
            // there's no way to know when the kerbal's mission actually began,
            // but the current time is a reasonable fallback.

            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                foreach (ProtoCrewMember kerbal in vessel.GetVesselCrew())
                {
                    kerbal.SetOnMission(Planetarium.GetUniversalTime());
                }
            }
        }
    }

    //   HighLogic.CurrentGame.Parameters.CustomParams<CrewRandRCustomParams>()

    public class CrewRandRCustomParams : GameParameters.CustomParameterNode
    {
        public override string Title { get { return ""; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Crew R&R"; } }
        public override string DisplaySection { get { return "Crew R&R"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }

#if false
        [GameParameters.CustomParameterUI("Mod Enabled?",
            toolTip = "Changing this requires restarting the game")]

#endif
        public bool enabled = true;

        [GameParameters.CustomIntParameterUI("Max Short Mission Length (hours)", minValue = 1, maxValue = 24,
    toolTip = "Max time to be considered a 'short' mission")]
        public int ShortMissionMaxLength = 4;


        [GameParameters.CustomIntParameterUI("Minimum vacation hours for Short mission", minValue = 1, maxValue = 24,
            toolTip = "Minimum time for vacation for a short mission, overrides the calculated vacation")]
        public int MinimumVacationHours = 1;


        [GameParameters.CustomIntParameterUI("Maximum vacation hours for Short mission", minValue = 1, maxValue = 24,
            toolTip = "Maximum time for vacation for a short mission, overrides the calculated vacation")]
        public int MaximumVacationHours = 6;



        [GameParameters.CustomParameterUI("Automatically select crew first crewable part?")]
        public bool AssignCrews = true;

        [GameParameters.CustomParameterUI("Automatically select crew for all crewable parts?")]
        public bool AssignAllCrews = true;

        // The following crazy code is due to a bug introduced in 1.2.2
        // See this link for details:  http://forum.kerbalspaceprogram.com/index.php?/topic/7542-the-official-unoffical-quothelp-a-fellow-plugin-developerquot-thread/&page=100#comment-2887044

        // [GameParameters.CustomFloatParameterUI("Base vacation rate", asPercentage = true)]
        //public float VacationScalar = .1f;       


        public float vacationScalar = 0.1F;
        [GameParameters.CustomFloatParameterUI("Base vacation rate (%)", displayFormat = "N0", minValue = 0, maxValue = 100, stepCount = 1, asPercentage = false,
            toolTip = "The vacation time will be calculated by multiplying the mission length by this")]
        public float VacationScalar
        {
            get { return vacationScalar * 100; }
            set { vacationScalar = value / 100.0f; }
        }

        [GameParameters.CustomIntParameterUI("Minimum vacation days", minValue = 1, maxValue = 100,
            toolTip = "Minimum time for vacation, overrides the calculated vacation")]
        public int MinimumVacationDays = 7;         


        [GameParameters.CustomIntParameterUI("Maximum vacation days", minValue = 1, maxValue = 100,
            toolTip = "Maximum time for vacation, overrides the calculated vacation")]
        public int MaximumVacationDays = 28;          


        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true;
        }
        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (MinimumVacationDays > MaximumVacationDays)
                MaximumVacationDays = MinimumVacationDays;

            if (MinimumVacationHours > MaximumVacationHours)
                MaximumVacationHours = MinimumVacationHours;


            return true;
        }
        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }

}
