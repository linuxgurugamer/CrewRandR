﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;

// Include this file in your project in order to support a soft-dependency on CrewQ.
// Do not edit this file.
// Example usage: CrewQ.API.SuppressCrew()
namespace CrewQ
{
    public class API
    {
        private static bool? _available = null;
        private static Type _type = null;
        private static object _instance;

        /// <summary>
        /// This indicates if CrewQ is loaded
        /// </summary>
        public static bool Available
        {
            get
            {
                if (_available == null)
                {
                    _type = AssemblyLoader.loadedAssemblies
                                          .Select(a => a.assembly.GetExportedTypes())
                                          .SelectMany(t => t)
                                          .FirstOrDefault(t => t.FullName == "CrewQ.CrewQ");

                    _available = _type != null;
                }
                return (bool)_available;
            }
        }        

        /// <summary>
        /// Returns the Kerbals who are allowed to go on missions, unsorted.
        /// </summary>
        public static IEnumerable<ProtoCrewMember> AvailableCrew
        {
            get
            {
                return (IEnumerable<ProtoCrewMember>) getProperty("AvailableCrew");          
            }
        }

        /// <summary>
        /// Returns the Kerbals who are not allowed to go on missions.
        /// </summary>
        public static IEnumerable<ProtoCrewMember> UnavailableCrew
        {
            get
            {
                return (IEnumerable<ProtoCrewMember>) getProperty("UnavailableCrew");
            }
        }

        /// <summary>
        /// Returns the Kerbals who are allowed to go on missions, sorted inexperienced first.
        /// </summary>
        public static IEnumerable<ProtoCrewMember> NewbieCrew
        {
            get
            {
                return (IEnumerable<ProtoCrewMember>)getProperty("NewbieCrew");
            }
        }

        /// <summary>
        /// Returns the Kerbals who are allowed to go on missions, sorted veterans first.
        /// </summary>
        public static IEnumerable<ProtoCrewMember> VeteranCrew
        {
            get
            {
                return (IEnumerable<ProtoCrewMember>)getProperty("VeteranCrew");
            }
        }

        /// <summary>
        /// Hides any kerbals who aren't allowed on missions from the vanilla available crew lists
        /// </summary>
        public void HideVacationingCrew()
        {
            invokeMethod("HideVacationingCrew");
        }

        /// <summary>
        /// Reverses a previous call to HideVacationingCrew
        /// </summary>
        public void ShowVacationingCrew()
        {
            invokeMethod("ShowVacationingCrew");
        }

        // Generic accessors
        internal static object Instance
        {
            get
            {
                if (Available && _instance == null)
                {
                    _instance = _type.GetProperty("Instance").GetValue(null, null);
                }

                return _instance;
            }
        }

        internal static object getProperty(string name, object[] indexes = null)
        {
            if (Available)
            {
                System.Reflection.PropertyInfo _property = _type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance);
                return _property.GetValue(Instance, indexes);
            }
            else
            {
                return null;
            }
        }

        internal static object invokeMethod(string name, object[] parameters = null)
        {
            if (Available)
            {
                MethodInfo _method = _type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
                return _method.Invoke(Instance, parameters);
            }
            else
            {
                return null;
            }
        }
    }

    public static class APIExtensions
    {
        /// <summary>
        /// Returns the time (in seconds) at which this Kerbal is or was eligible for missions again.
        /// </summary>
        public static double GetVacationTimer(this ProtoCrewMember kerbal)
        {
            return (double)API.invokeMethod("GetVacationTimerInternal", new object[] { kerbal });
        }

        /// <summary>
        /// Returns the vacation state of this Kerbal
        /// </summary>
        public static bool OnVacation(this ProtoCrewMember kerbal)
        {
            return (bool)API.invokeMethod("OnVacationInternal", new object[] { kerbal });
        }

        /// <summary>
        /// Set the time at which this Kerbal will be eligible for missions again.
        /// </summary>
        /// <param name="timeout">Time (in seconds) at which this Kerbal will be eligible for missions.</param>
        public static void SetVacationTimer(this ProtoCrewMember kerbal, double timeout)
        {
            API.invokeMethod("SetVacationTimerInternal", new object[] { kerbal, timeout });
        }
    }
}