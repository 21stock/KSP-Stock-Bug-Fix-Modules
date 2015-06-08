﻿/*
 * This module written by Claw. For more details, please visit
 * http://forum.kerbalspaceprogram.com/threads/97285
 * 
 * This mod is covered under the CC-BY-NC-SA license. See the readme.txt for more details.
 * (https://creativecommons.org/licenses/by-nc-sa/4.0/)
 * 
 *
 * ModuleRSASFix - Written for KSP v1.0
 * 
 * - Fixes overreaction by the SAS for small vessels with excess torque/RCS control.
 * - Somewhat reduces wobbly craft, but doesn't eliminate it
 * - (Plus) Gives tweakable RSAS adjustment parameters
 * 
 * Change Log:
 * - v00.01  (7 Jun 15)   Initial Release
 * 
 */

using UnityEngine;
using KSP;

namespace ClawKSP
{
    public class PilotRSASFix : PartModule
    {
        [KSPField(guiName = "Min Response", isPersistant = true, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(minValue = 0.05f, maxValue = 1.0f, stepIncrement = 0.05f)]
        public float minResponseLimit = 0.3f;

        [KSPField(guiName = "Min Clamp", isPersistant = true, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(minValue = 0.1f, maxValue = 0.3f, stepIncrement = 0.01f)]
        public float minClamp = 0.2f;

        [KSPField(guiName = "Threshold", isPersistant = true, guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(minValue = 0.1f, maxValue = 0.9f, stepIncrement = 0.1f)]
        public float threshold = 0.3f;

        //[KSPField(guiName = "Response Limit", isPersistant = false, guiActive = true, guiActiveEditor = true)]
        public float responseLimit = 1f;

        //[KSPField(guiName = "Clamp", isPersistant = false, guiActive = true, guiActiveEditor = true)]
        public float Clamp = 1f;


        [KSPField(isPersistant = false)]
        public bool plusEnabled = false;

        private bool isActiveGUI = false;

        private static Vessel setVessel;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Debug.Log(moduleName + ".Start(): v00.01");

            GameEvents.onVesselChange.Add(DisableGUI);
            DisableGUI(null);
            SetupStockPlus();
        }

        public void LateUpdate()
        {
            setVessel = null;
        }

        public void DisableGUI(Vessel v)
        {
            Fields["minResponseLimit"].guiActive = false;
            Fields["minResponseLimit"].guiActiveEditor = false;
            Fields["minClamp"].guiActive = false;
            Fields["minClamp"].guiActiveEditor = false;
            Fields["threshold"].guiActive = false;
            Fields["threshold"].guiActiveEditor = false;
            isActiveGUI = false;
        }

        private void SetupStockPlus()
        {
            if (StockPlusController.plusActive == false || plusEnabled == false)
            {
                plusEnabled = false;
                return;
            }

            Debug.Log(moduleName + " StockPlus Enabled");
        }

        public void FixedUpdate()
        {
            if (setVessel == part.vessel)
            {
                return;
            }

            setVessel = part.vessel;

            if (plusEnabled && !isActiveGUI)
            {
                Fields["minResponseLimit"].guiActive = true;
                Fields["minResponseLimit"].guiActiveEditor = true;
                Fields["minClamp"].guiActive = true;
                Fields["minClamp"].guiActiveEditor = true;
                Fields["threshold"].guiActive = true;
                Fields["threshold"].guiActiveEditor = true;
                isActiveGUI = true;
            }

            if (null != setVessel)
            {
                if (null != setVessel.Autopilot.RSAS.pidPitch)
                {
                    Vector3 vT = setVessel.ReferenceTransform.InverseTransformDirection(setVessel.Autopilot.RSAS.targetOrientation);
                    float dP = 90f - Mathf.Atan2(vT.y, vT.z) * (180f / 3.14159265359f);
                    if (dP > 180f) { dP -= 360f; }
                    float dY = Mathf.Atan2(vT.x, vT.y) * (180f / 3.14159265359f);


                    float dA = Mathf.Sqrt((dP * dP) + (dY * dY));
                    Clamp = 1.0f;
                    if (dA < 5)
                    {
                        if (setVessel.angularVelocity.magnitude < threshold)
                        {
                            Clamp = dA / 5f;
                            if (Clamp < minClamp) { Clamp = minClamp; }
                        }
                    }

                    if (Clamp < 1.0f)
                    {
                        responseLimit -= 0.01f;
                        if (responseLimit < minResponseLimit)
                        {
                            responseLimit = minResponseLimit;
                        }
                    }
                    else
                    {
                        responseLimit += 0.01f;
                        if (responseLimit > 1.0f)
                        {
                            responseLimit = 1.0f;
                        }
                    }

                    // Original RSAS Values
                    //   pitch (18.3f, 1.3f, 0.5f, 1f);
                    //   roll (6f, 0.25f, 0.025f, 1f);
                    //   yaw (18.3f, 1.3f, 0.5f, 1f);
                    FlightGlobals.ActiveVessel.Autopilot.RSAS.pidPitch.ReinitializePIDsOnly(18.3f * responseLimit, 0f * responseLimit, 0.5f * responseLimit);
                    FlightGlobals.ActiveVessel.Autopilot.RSAS.pidRoll.ReinitializePIDsOnly(6f * responseLimit, 0f * responseLimit, 0.025f * responseLimit);
                    FlightGlobals.ActiveVessel.Autopilot.RSAS.pidYaw.ReinitializePIDsOnly(18.3f * responseLimit, 0f * responseLimit, 0.5f * responseLimit);
                    setVessel.Autopilot.RSAS.pidPitch.Clamp(Clamp);
                    setVessel.Autopilot.RSAS.pidRoll.Clamp(Clamp);
                    setVessel.Autopilot.RSAS.pidYaw.Clamp(Clamp);
                }
            }
        }
    }
}
