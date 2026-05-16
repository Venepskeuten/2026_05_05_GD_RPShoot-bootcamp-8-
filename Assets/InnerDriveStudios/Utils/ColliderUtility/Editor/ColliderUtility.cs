using System;
using UnityEditor;
using UnityEngine;

namespace InnerDriveStudios.Util
{
    /**
     * Allows you to quickly remove all colliders from a (nested) (prefab) GameObject
     * and to quickly re-add one bounding box, sphere or capsule collider.
     * 
     * @author J.C.Wichman - InnerDriveStudios.com
     */
    public static class ColliderUtility
    {
        private const string SUB_MENU = "Colliders/";
		private const string HeightFitModePath = Settings.MENU_PATH + SUB_MENU + "Capsule Height Fit Mode";
		private const string RadiusFitModePath = Settings.MENU_PATH + SUB_MENU + "Capsule && Sphere Radius Fit Mode";

        private enum FitMode { Inside, Outside, Midway };
        private static FitMode heightFitMode = FitMode.Inside;
        private static FitMode radiusFitMode = FitMode.Inside;

        static ColliderUtility() {
            UpdateHeightFitModeCheckboxes();
            UpdateRadiusFitModeCheckboxes();
        }

		private static void UpdateHeightFitModeCheckboxes()
		{
            Menu.SetChecked(HeightFitModePath + "/" + FitMode.Inside,  heightFitMode == FitMode.Inside);	
            Menu.SetChecked(HeightFitModePath + "/" + FitMode.Outside, heightFitMode == FitMode.Outside);	
            Menu.SetChecked(HeightFitModePath + "/" + FitMode.Midway,  heightFitMode == FitMode.Midway);	
		}

		private static void UpdateRadiusFitModeCheckboxes()
		{
			Menu.SetChecked(RadiusFitModePath + "/" + FitMode.Inside,  radiusFitMode == FitMode.Inside);
			Menu.SetChecked(RadiusFitModePath + "/" + FitMode.Outside, radiusFitMode == FitMode.Outside);
			Menu.SetChecked(RadiusFitModePath + "/" + FitMode.Midway,  radiusFitMode == FitMode.Midway);
		}

		private const string AutoRemoveCollidersBeforeAddingPath = Settings.MENU_PATH + SUB_MENU + "Auto remove colliders before adding";

		[MenuItem(AutoRemoveCollidersBeforeAddingPath, priority = -100)]
		private static void AutoRemoveCollidersBeforeAdding()
		{
			Menu.SetChecked(AutoRemoveCollidersBeforeAddingPath, !Menu.GetChecked(AutoRemoveCollidersBeforeAddingPath));
		}

		[MenuItem(HeightFitModePath + "/Inside", priority = -102)]
        private static void HeightFitModeSetterInside () { SetHeightFitMode(FitMode.Inside);  }
		[MenuItem(HeightFitModePath + "/Outside", priority = -101)]
        private static void HeightFitModeSetterOutside () { SetHeightFitMode(FitMode.Outside);  }
		[MenuItem(HeightFitModePath + "/Midway", priority = -100)]
        private static void HeightFitModeSetterMidway () { SetHeightFitMode(FitMode.Midway);  }

		[MenuItem(RadiusFitModePath + "/Inside", priority = -102)]
		private static void RadiusFitModeSetterInside() { SetRadiusFitMode(FitMode.Inside); }
		[MenuItem(RadiusFitModePath + "/Outside", priority = -101)]
		private static void RadiusFitModeSetterOutside() { SetRadiusFitMode(FitMode.Outside); }
		[MenuItem(RadiusFitModePath + "/Midway", priority = -100)]
		private static void RadiusFitModeSetterMidway() { SetRadiusFitMode(FitMode.Midway); }

		private static void SetHeightFitMode(FitMode pFitMode)
		{
            heightFitMode = pFitMode;
            UpdateHeightFitModeCheckboxes();
		}

		private static void SetRadiusFitMode(FitMode pFitMode)
		{
			radiusFitMode = pFitMode;
			UpdateRadiusFitModeCheckboxes();
		}

		[MenuItem(Settings.MENU_PATH + SUB_MENU + "Remove all colliders from selected objects")]
		private static void RemoveAllCollidersFromSelectedObjects()
		{
			//Loop over all selected objects, both hierarchy and project window ...
			foreach (GameObject gameObject in Selection.gameObjects)
			{
                foreach (Collider collider in gameObject.GetComponentsInChildren<Collider>())
                {
                    Undo.DestroyObjectImmediate(collider);
                }
			}
		}

        [MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding box collider")]
        private static void AddBoundingBoxCollider()
        {
            if (Menu.GetChecked(AutoRemoveCollidersBeforeAddingPath)) RemoveAllCollidersFromSelectedObjects();

            foreach (GameObject gameObject in Selection.gameObjects)
            {
                AddBoundingBoxCollider(gameObject);
            }
        }

        private static void AddBoundingBoxCollider(GameObject pGameObject)
		{
            if (!Common.GetLocalBounds(pGameObject.transform, out Bounds localBounds)) return;

			BoxCollider boxCollider = Undo.AddComponent<BoxCollider>(pGameObject);
            boxCollider.center = localBounds.center;
			boxCollider.size = localBounds.size;
        }


        [MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding sphere collider")]
        private static void AddBoundingSphereCollider()
        {
            if (Menu.GetChecked(AutoRemoveCollidersBeforeAddingPath)) RemoveAllCollidersFromSelectedObjects();

            foreach (GameObject gameObject in Selection.gameObjects)
            {
                AddBoundingSphereCollider(gameObject);
            }
        }

        private static void AddBoundingSphereCollider(GameObject pGameObject)
        {
            if (!Common.GetLocalBounds(pGameObject.transform, out Bounds localBounds)) return;

            SphereCollider sphereCollider = Undo.AddComponent<SphereCollider>(pGameObject);
            sphereCollider.center = localBounds.center;

            //This next part required a lot of experimentation and tinkering in the Unity Editor
            //Often the radius would look right and be right in most situation, but then some kind
            //of shitty exception to the rule would occur. 

            //Required radius is the magnitude of the scaled global extents
            Vector3 lossyScale = pGameObject.transform.lossyScale;
            Vector3 scaledBounds = Vector3.Scale(localBounds.extents, lossyScale);
            float scaledRequiredRadiusOutside = scaledBounds.magnitude;
            float scaledRequiredRadiusInside = Mathf.Max(scaledBounds.x, Mathf.Max(scaledBounds.y, scaledBounds.z));

            float radiusToUse = radiusFitMode switch
			{
				FitMode.Inside => scaledRequiredRadiusInside,
				FitMode.Outside => scaledRequiredRadiusOutside,
				FitMode.Midway => (scaledRequiredRadiusInside + scaledRequiredRadiusOutside) / 2,
				_ => throw new NotImplementedException()
			};
            
            //However unity automatically scales the set radius using the biggest scaling factor, 
            //so we need to undo that...
            sphereCollider.radius = radiusToUse / Mathf.Max(lossyScale.x, Mathf.Max(lossyScale.y, lossyScale.z));
        }

        [MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding capsule collider")]
        private static void AddBoundingCapsuleCollider()
        {
            if (Menu.GetChecked(AutoRemoveCollidersBeforeAddingPath)) RemoveAllCollidersFromSelectedObjects();

            foreach (GameObject gameObject in Selection.gameObjects)
            {
                AddBoundingCapsuleCollider(gameObject);
            }
        }

        private static void AddBoundingCapsuleCollider(GameObject pGameObject)
		{
            if (!Common.GetLocalBounds(pGameObject.transform, out Bounds localBounds)) return;

            CapsuleCollider capsuleCollider = Undo.AddComponent<CapsuleCollider>(pGameObject);

            capsuleCollider.center = localBounds.center;

            //This next part required a lot of experimentation and tinkering in the Unity Editor
            //Often the radius OR the height would look right and be right in most situation,
            //but then some kind of shitty exception to the rule would occur. 

            //The reason documentation is lacking for most of the code is that although it is 
            //clear what it is doing, the reason WHY is "because experimentation told me so"
            //All of this is related to 

            Vector3 lossyScale = pGameObject.transform.lossyScale;
            Vector3 scaledBounds = Vector3.Scale(localBounds.extents, lossyScale);

            int direction = 0;              //the direction of the capsule collider 0 = x, 1 = y, 2 = z
            float height = 0;               //the base height value (the x, y, z extent of the bounding box we are using, depending on our direction)
            float radiusDivider = 0;        //radius needs to be divived by the max value of two scaling values (xy, yz, xz) based on the direction
            float heightDivider = 0;        //height needs to be divided by the left over scaling value (x, y, z) based on the direction

            Vector3 extents = localBounds.extents;
            if (scaledBounds.x >= scaledBounds.y && scaledBounds.x >= scaledBounds.z)
            {
                direction = 0;
                height = extents.x;

                radiusDivider = Mathf.Max(lossyScale.y, lossyScale.z);
                heightDivider = lossyScale.x;

                //zero out this value, because it shouldn't be taken into account when calculating the capsule radius
                scaledBounds.x = 0; 
            }
            else if (scaledBounds.y >= scaledBounds.x && scaledBounds.y >= scaledBounds.z)
            {
                direction = 1;
                height = extents.y;

                radiusDivider = Mathf.Max(lossyScale.x, lossyScale.z);
                heightDivider = lossyScale.y;

                //zero out this value, because it shouldn't be taken into account when calculating the capsule radius
                scaledBounds.y = 0;
            }
            else if (scaledBounds.z >= scaledBounds.x && scaledBounds.z >= scaledBounds.y)
            {
                direction = 2;
                height = extents.z;

                radiusDivider = Mathf.Max(lossyScale.x, lossyScale.y);
                heightDivider = lossyScale.z;

                //zero out this value, because it shouldn't be taken into account when calculating the capsule radius
                scaledBounds.z = 0;
            }

            capsuleCollider.direction = direction;

			float scaledRequiredRadiusOutside = scaledBounds.magnitude;
			float scaledRequiredRadiusInside = Mathf.Max(scaledBounds.x, Mathf.Max(scaledBounds.y, scaledBounds.z));

			float radiusToUse = radiusFitMode switch
			{
				FitMode.Inside => scaledRequiredRadiusInside,
				FitMode.Outside => scaledRequiredRadiusOutside,
				FitMode.Midway => (scaledRequiredRadiusInside + scaledRequiredRadiusOutside) / 2,
				_ => throw new NotImplementedException()
			};

			capsuleCollider.radius = radiusToUse / radiusDivider;

            float requiredHeightInside = 2 * height;
            float requiredHeightOutside = 2 * (height + (capsuleCollider.radius * radiusDivider / heightDivider));

            float heightToUse = heightFitMode switch
			{
				FitMode.Inside => requiredHeightInside,
				FitMode.Outside => requiredHeightOutside,
				FitMode.Midway => (requiredHeightInside + requiredHeightOutside) / 2,
				_ => throw new NotImplementedException()
			};

            capsuleCollider.height = heightToUse;
        }

        [MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding capsule collider", true)]
		[MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding box collider", true)]
		[MenuItem(Settings.MENU_PATH + SUB_MENU + "Add bounding sphere collider", true)]
        [MenuItem(Settings.MENU_PATH + SUB_MENU + "Remove all colliders from selected objects", true)]
        private static bool IsGameObjectSelected()
		{
			return Selection.activeGameObject != null;
		}

	}
}
