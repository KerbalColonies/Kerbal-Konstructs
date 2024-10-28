using KerbalKonstructs.Core;
using KerbalKonstructs.Modules;
using KerbalKonstructs.UI;
using System;
using UnityEngine;

namespace KerbalKonstructs
{
    public static class API
    {

        internal static Action<GameObject> OnBuildingSpawned = delegate { };
        internal static Action<GroupCenter> OnGroupSaved = delegate { };

        public static string SpawnObject(string modelName)
        {
            StaticModel model = StaticDatabase.GetModelByName(modelName);
            if (model != null)
            {
                return CareerEditor.instance.SpawnInstance(model, 3f, KerbalKonstructs.instance.GetCurrentBody().transform.InverseTransformPoint(FlightGlobals.ActiveVessel.transform.position));
            }
            else
            {
                Log.UserError("API:SpawnObject: Could not find selected KK-Model named: " + modelName);
                return null;
            }
        }

        public static void RemoveStatic(string uuid)
        {
            if (StaticDatabase.instancedByUUID.ContainsKey(uuid))
            {
                StaticDatabase.instancedByUUID[uuid].Destroy();
            }
            else
            {
                Log.UserWarning("API:RemoveObject: Can´t find a static with the UUID: " + uuid);
            }
        }

        public static void HighLightStatic(string uuid, Color color)
        {
            if (StaticDatabase.instancedByUUID.ContainsKey(uuid))
            {
                StaticDatabase.instancedByUUID[uuid].HighlightObject(color);
            }
            else
            {
                Log.UserWarning("API:Highlight: Can´t find a static with the UUID: " + uuid);
            }
        }

        public static string GetModelTitel(string uuid)
        {
            if (StaticDatabase.instancedByUUID.ContainsKey(uuid))
            {
                return StaticDatabase.instancedByUUID[uuid].model.title;
            }
            else
            {
                Log.UserWarning("API:GetModelTitel: Can´t find a static with the UUID: " + uuid);
                return null;
            }
        }

        public static void SetEditorRange(float newRange)
        {
            CareerEditor.maxEditorRange = newRange;
        }


        public static GameObject GetGameObject(string uuid)
        {
            if (StaticDatabase.instancedByUUID.ContainsKey(uuid))
            {
                return (StaticDatabase.instancedByUUID[uuid]).gameObject;
            }
            else
            {
                Log.UserWarning("API:GetGameObject: Can´t find a static with the UUID: " + uuid);
                return null;
            }
        }

        public static string PlaceStatic(string modelName, string bodyName, double lat, double lng, float alt, float rotation, bool isScanable = false, string groupname = "SaveGame")
        {
            StaticModel model = StaticDatabase.GetModelByName(modelName);
            if (model != null)
            {
                StaticInstance instance = new StaticInstance();
                instance.isInSavegame = true;

                instance.heighReference = HeightReference.Terrain;

                //instance.mesh = UnityEngine.Object.Instantiate(model.prefab);
                instance.RadiusOffset = alt;
                instance.CelestialBody = ConfigUtil.GetCelestialBody(bodyName);
                if (StaticDatabase.HasGroupCenter(groupname))
                {
                    instance.Group = groupname;
                }
                else
                {
                    instance.Group = "SaveGame";
                }
                instance.RadialPosition = KKMath.GetRadiadFromLatLng(instance.CelestialBody, lat, lng);
                instance.RotationAngle = rotation;
                instance.Orientation = Vector3.up;
                instance.VisibilityRange = (PhysicsGlobals.Instance.VesselRangesDefault.flying.unload + 3000);
                instance.RefLatitude = lat;
                instance.RefLongitude = lng;

                instance.model = model;
                instance.configPath = null;
                instance.configUrl = null;

                instance.isScanable = isScanable;

                instance.Orientate();
                instance.Activate();

                return instance.UUID;
            }

            Log.UserError("API:PlaceStatic: StaticModel not found in Database: " + modelName);
            return null;
        }

        #region groups

        public static bool OpenGroupEditor(string groupName, string bodyName = null)
        {
            if (bodyName == null)
            {
                bodyName = StaticDatabase.lastActiveBody.name;
            }

            string groupNameB = $"{bodyName}_{groupName}";
            if (StaticDatabase.HasGroupCenter(groupNameB))
            {
                EditorGUI.CloseEditors();
                MapDecalEditor.Instance.Close();
                GroupEditor.instance.Close();
                GroupEditor.selectedGroup = StaticDatabase.GetGroupCenter(groupNameB);
                GroupEditor.instance.Open();
                return true;
            }
            return false;

        }

        public static bool CreateGroup(string groupName, Vector3 RadialPosition = default(Vector3))
        {
            if (!StaticDatabase.HasGroupCenter(groupName))
            {
                GroupCenter groupCenter = new GroupCenter
                {
                    RadialPosition = (RadialPosition == default(Vector3)) ? FlightGlobals.currentMainBody.transform.InverseTransformPoint(FlightGlobals.ActiveVessel.transform.position) : RadialPosition,
                    Group = groupName,
                    CelestialBody = FlightGlobals.currentMainBody
                };
                groupCenter.Spawn();
                KSPLog.print(groupCenter.Group);
                return true;
            }
            Log.UserWarning($"API:CreateGroup: group with name {groupName} already exists.");
            return false;
        }

        public static bool RemoveGroup(string groupName, string bodyName = null)
        {
            if (bodyName == null)
            {
                bodyName = StaticDatabase.lastActiveBody.name;
            }

            string groupNameB = $"{bodyName}_{groupName}";
            if (StaticDatabase.HasGroupCenter(groupNameB))
            {
                StaticsEditorGUI.SetActiveGroup(StaticDatabase.GetGroupCenter(groupNameB));
                GroupEditor.selectedGroup = StaticDatabase.GetGroupCenter(groupNameB);
                GroupEditor.instance.DeleteGroupCenter();
                return true;
            }
            Log.UserWarning($"API:RemoveGroup: group with name {groupName} does not exists.");
            return false;
        }

        /// <summary>
        /// Copies all statics from a group to another group
        /// </summary>
        public static bool CopyGroup(string toGroupName, string fromGroupName, string bodyName = null)
        {
            if (bodyName == null) { bodyName = StaticDatabase.lastActiveBody.name; }
            string toGroupNameB = $"{bodyName}_{toGroupName}";
            string fromGroupNameB = $"{bodyName}_{fromGroupName}";
            if (StaticDatabase.HasGroupCenter(toGroupNameB) && StaticDatabase.HasGroupCenter(fromGroupNameB))
            {
                StaticsEditorGUI.SetActiveGroup(StaticDatabase.GetGroupCenter(toGroupNameB));
                StaticsEditorGUI.GetActiveGroup().CopyGroup(StaticDatabase.GetGroupCenter(fromGroupNameB));
                return true;
            }
            Log.UserWarning($"API:CopyGroup: at least one of the groups does not exists.");
            return false;
        }

        /// <summary>
        /// Unfinished, currently it does nothing
        /// </summary>
        public static void MoveGroup(string groupname, CelestialBody body, Vector3 radialPosition, Vector3 orientation, bool seaLevelAsReference = false) { }

        public static bool AddStaticToGroup(string uuid, string groupName, string bodyName = null)
        {
            if (bodyName == null)
            {
                bodyName = StaticDatabase.lastActiveBody.name;
            }
            if (StaticDatabase.instancedByUUID.ContainsKey(uuid) && StaticDatabase.HasGroupCenter($"{bodyName}_{groupName}"))
            {
                StaticInstance instance = StaticDatabase.instancedByUUID[uuid];
                instance.Group = groupName;
                return true;
            }
            Log.UserWarning($"API:AddStaticToGroup: the uuid and/or the groupname weren't found.");
            return false;
        }

        #endregion

        public static bool AddEnterTriggerCallback(string uuid, Action<Part> myFunction)
        {
            if (StaticDatabase.instancedByUUID.ContainsKey(uuid))
            {
                //do stuff
                StaticInstance instance = StaticDatabase.instancedByUUID[uuid];

                KKCallBack controller = instance.gameObject.GetComponent<KKCallBack>();
                if (controller == null)
                {
                    Log.UserWarning("API:AddEnterTriggerCallback: Can´t find a CallBack controller");
                    return false;
                }

                controller.RegisterEnterFunc(myFunction);
                return true;
            }
            else
            {
                Log.UserWarning("API:AddEnterTriggerCallback: Can´t find a static with the UUID: " + uuid);
                return false;
            }
        }

        public static bool AddStayTriggerCallback(string uuid, Action<Part> myFunction)
        {
            if (StaticDatabase.instancedByUUID.ContainsKey(uuid))
            {
                //do stuff
                StaticInstance instance = StaticDatabase.instancedByUUID[uuid];

                KKCallBack controller = instance.gameObject.GetComponent<KKCallBack>();
                if (controller == null)
                {
                    Log.UserWarning("API:AddStayTriggerCallback: Can´t find a CallBack controller");
                    return false;
                }

                controller.RegisterStayFunc(myFunction);
                return true;
            }
            else
            {
                Log.UserWarning("API:AddStayTriggerCallback: Can´t find a static with the UUID: " + uuid);
                return false;
            }
        }

        public static bool AddExitTriggerCallback(string uuid, Action<Part> myFunction)
        {
            if (StaticDatabase.instancedByUUID.ContainsKey(uuid))
            {
                //do stuff
                StaticInstance instance = StaticDatabase.instancedByUUID[uuid];

                KKCallBack controller = instance.gameObject.GetComponent<KKCallBack>();
                if (controller == null)
                {
                    Log.UserWarning("API:AddExitTriggerCallback: Can´t find a CallBack controller");
                    return false;
                }

                controller.RegisterExitFunc(myFunction);
                return true;
            }
            else
            {
                Log.UserWarning("API:AddExitTriggerCallback: Can´t find a static with the UUID: " + uuid);
                return false;
            }
        }

        public static void Save()
        {
            KerbalKonstructs.instance.SaveObjects();
        }

        public static void RegisterOnBuildingSpawned(Action<GameObject> action)
        {
            OnBuildingSpawned += action;
        }

        public static void UnregisterOnBuildingSpawned(Action<GameObject> action)
        {
            OnBuildingSpawned -= action;
        }

        public static void RegisterOnGroupSaved(Action<GroupCenter> action)
        {
            OnGroupSaved += action;
        }
        public static void UnRegisterOnGroupSaved(Action<GroupCenter> action)
        {
            OnGroupSaved -= action;
        }
    }
}
