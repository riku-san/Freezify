//-------------------------------------------------------------
// Author: Riku_san
// Date Created: 9/21/2022
// Description: Contains all functions for the Freezify
//         Unity Editor tool. More information in README.txt
//-------------------------------------------------------------
using UnityEngine;
using UnityEngine.Animations;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System;

public class Freezify : EditorWindow
{
    bool meshFold;
    bool debug = false;
    int meshCount = 0;
    int _meshCount = 0;
    Vector2 scrollPos;

    GameObject OA_Armature, FA_Armature, freezeRoot;
    GameObject[] OA_Meshes, FA_Meshes;
    AnimatorController[] animators = new AnimatorController[5];

    // Error code enumerator for returns
    private enum ErrorCodes
    {
        none = 0,
        animatorMissing,
        animatorClipsMissing,
        animationCopyError,
        animatorCopyError,
        faNULL,
        oaNULL,
        bothNULL,
        oaArmNULL,
        faArmNULL
    };

    [MenuItem("Tools/Riku's Tools/Freezify")]
    public static void ShowWindow()
    {
        GetWindow<Freezify>("Freezify");
    }

    private void OnGUI()
    {
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("Constraint Setup", EditorStyles.boldLabel);
        GUILayout.Space(5.0f);

        // Original Avatar parameter GUI setup
        OA_Armature = EditorGUILayout.ObjectField("Original Hip Bone", OA_Armature, typeof(GameObject), true) as GameObject;
        FA_Armature = EditorGUILayout.ObjectField("Freeze Hips Bone", FA_Armature, typeof(GameObject), true) as GameObject;

        // Fold out layer for mesh assigning
        meshFold = EditorGUILayout.Foldout(meshFold, "Meshes");
        if (meshFold) 
        {
            // Gets a size to make a new array
            EditorGUI.indentLevel++;
            meshCount = EditorGUILayout.IntField("Number of Meshes", meshCount);
            if (meshCount > 0)
            {
                // If the array size has changed, makes a new array
                if (meshCount != _meshCount)
                {
                    OA_Meshes = new GameObject[meshCount];
                    FA_Meshes = new GameObject[meshCount];
                    _meshCount = meshCount;
                }


                // Displays each mesh for meshCount and assigns it
                EditorGUI.indentLevel++;
                for (int i = 0; i < meshCount; i++)
                {
                    OA_Meshes[i] = EditorGUILayout.ObjectField("Original Mesh: " + i, OA_Meshes[i], typeof(GameObject), true) as GameObject;
                    FA_Meshes[i] = EditorGUILayout.ObjectField("Freeze Mesh: " + i, FA_Meshes[i], typeof(GameObject), true) as GameObject;
                    
                    // Adds space in between the meshes except for the last one
                    if (i != meshCount - 1)
                        GUILayout.Space(6.0f);
                }

                EditorGUI.indentLevel--;
            }
            else if (meshCount < 0) GUILayout.Label("Error: Invalid Size! Number must be greater than 0!", EditorStyles.helpBox);

            EditorGUI.indentLevel--;
        }


        // Animators
        GUILayout.Space(15.0f);
        GUILayout.Label("Avatar Animators", EditorStyles.boldLabel);
        GUILayout.Space(5.0f);

        freezeRoot = EditorGUILayout.ObjectField("Freeze Root", freezeRoot, typeof(GameObject), true) as GameObject;

        GUILayout.Space(5.0f);

        // Assigns the animator components for animation making
        animators[0] = EditorGUILayout.ObjectField("Base Animator", animators[0], typeof(AnimatorController), false) as AnimatorController;
        animators[1] = EditorGUILayout.ObjectField("Additive Animator", animators[1], typeof(AnimatorController), false) as AnimatorController;
        animators[2] = EditorGUILayout.ObjectField("Gesture Animator", animators[2], typeof(AnimatorController), false) as AnimatorController;
        animators[3] = EditorGUILayout.ObjectField("Action Animator", animators[3], typeof(AnimatorController), false) as AnimatorController;
        animators[4] = EditorGUILayout.ObjectField("FX Animator", animators[4], typeof(AnimatorController), false) as AnimatorController;

        // Gives the user the option to get debug feedback in case of errors
        GUILayout.Space(10.0f);
        debug = EditorGUILayout.Toggle("Enable Debug: ", debug);
        
        // Ends scroll-able area
        GUILayout.EndScrollView();

        // Puts the buttons at the bottom of the window
        GUILayout.FlexibleSpace();
        if (debug)
        {
            if (GUILayout.Button("Set up Constraints"))
                AvatarConstraintSetup();
            if (GUILayout.Button("Set up Avatar Animations"))
                AvatarAnimationSetup();

            // This is here to give this option to the user to only run this function
            // This function is run through AvatarAnimationSetup and may break if folders don't exist
            if (GUILayout.Button("Create Freezify Animation"))
                CreateFreezeAnimation();

            if (GUILayout.Button("Setup All"))
            {
                AvatarConstraintSetup();
                AvatarAnimationSetup();
            }

            GUILayout.Space(10.0f);
            if (GUILayout.Button("Reset Constraints"))
                AvatarReset();
        }
        else
        {
            if (GUILayout.Button("Freezify"))
            {
                AvatarConstraintSetup();
                AvatarAnimationSetup();
            }

            if (GUILayout.Button("Reset"))
                AvatarReset();
        }
    }

    private void AvatarConstraintSetup()
    {
        // Sets up the armature arrays for coding usage
        GameObject[] oaArmature = BoneArraySetup(OA_Armature);
        GameObject[] faArmature = BoneArraySetup(FA_Armature);
        GameObject[] length;

        // NULL check
        if (oaArmature == null || faArmature == null)
        {
            if (oaArmature == null)
                ErrorParse(ErrorCodes.oaArmNULL);
            if (faArmature == null)
                ErrorParse(ErrorCodes.faArmNULL);
            
            return;
        }

        // Fail-check this makes it so you can never
        // go out of bounds of the armature array
        if (oaArmature.Length > faArmature.Length)
            length = faArmature;
        else
            length = oaArmature;

        DEBUG_LOG(length.Length + "  OA: " + oaArmature.Length + "  FA: " + faArmature.Length);

        // Creates a Parent Constraint on the hip bone, and sets up the source
        ParentConstraint pc = faArmature[0].AddComponent<ParentConstraint>();
        ConstraintSource cs = new ConstraintSource();
        cs.sourceTransform = oaArmature[0].transform;
        cs.weight = 1.0f;

        // Alters settings of parent constraint
        pc.AddSource(cs);
        pc.constraintActive = true;
        pc.weight = 1.0f;
        pc.locked = true;

        // Goes through the original armature and 
        for (int i = 1; i < faArmature.Length; i++)
        {
            // If the name of the cur matches
            for (int j = 0; j < length.Length; j++)
            {
                if (faArmature[i].name != oaArmature[j].name)
                    continue;

                RotationConstraint rc = faArmature[i].AddComponent<RotationConstraint>();
                cs = new ConstraintSource();
                cs.sourceTransform = oaArmature[j].transform;
                cs.weight = 1.0f;

                rc.AddSource(cs);
                rc.constraintActive = true;
                rc.weight = 1.0f;
                rc.locked = true;

                // Finds a PhysBone Object, if it exists
                VRCPhysBone phb = faArmature[i].GetComponent<VRCPhysBone>();
                if (phb)
                    DestroyImmediate(phb, true);

                break;
            }
        }
    }

    private void AvatarAnimationSetup()
    {
        // Does a null check on the meshes
        // If any are found, parses and returns
        ErrorCodes err = NullCheck();
        if (err != ErrorCodes.none)
        {
            ErrorParse(err);
            return;
        }

        // Creates the directories used for storing new assets
        if (!Directory.Exists("Assets/Freezify"))
            AssetDatabase.CreateFolder("Assets", "Freezify");
        if (!Directory.Exists("Assets/Freezify/Animations"))
            AssetDatabase.CreateFolder("Assets/Freezify", "Animations");
        if (!Directory.Exists("Assets/Freezify/Animators"))
            AssetDatabase.CreateFolder("Assets/Freezify", "Animators");

        // Runs the code on each animator provided
        for (int i = 0; i < animators.Length; i++)
        {
            // Runs the Animation Setup function and parses the return value
            ErrorParse(AnimSetup(animators[i]));
        }

        // Runs the create animation function
        CreateFreezeAnimation();
    }

    private ErrorCodes AnimSetup(AnimatorController animator)
    {
        // Checks for null
        if (!animator)
            return ErrorCodes.animatorMissing;

        // Verifies animator isn't empty
        AnimationClip[] animClips = animator.animationClips;

        if (animClips.Length <= 0)
            return ErrorCodes.animatorClipsMissing;

        AnimationClip[] newClips = new AnimationClip[animClips.Length];

        // Creates new sub-directory to keep new animations/animators
        if (!Directory.Exists("Assets/Freezify/Animations/" + animator.name))
            AssetDatabase.CreateFolder("Assets/Freezify/Animations", animator.name);

        // Parameters for GUIDs
        string guid = "";
        long localID = 0;  // UNUSED PARAMETER
        string debug = "Animation Information:\n";
        debug += animator.name + ": animClips.Length = " + animClips.Length + '\n';

        // Goes through the animation clips, makes copies of them, and edits them
        for (int i = 0; i < animClips.Length; i++)
        {
            bool found = false;

            // Runs through the existing list of animations
            // Finds any duplicate animations and prevents it from loading the asset again
            for (int j = 0; j < i; j++)
            {
                if (newClips[j].name.TrimStart("freeze_".ToCharArray()) == animClips[i].name)
                {
                    newClips[i] = newClips[j];
                    found = true;
                    break;
                }
            }

            // If we have a duplicate, continues
            if (found)
                continue;

            // Gets the GUID of the animation
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(animClips[i].GetInstanceID(), out guid, out localID);
            string animPath = "Assets/Freezify/Animations/" + animator.name + "/" + "freeze_" + animClips[i].name + ".anim";

            // Creates a copy of the animation in a new folder, if failed, aborts operation
            if (!AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(guid), animPath))
            {
                DEBUG_LOG(animClips[i].name + ".anim Copy Failed! Aborting...");
                return ErrorCodes.animationCopyError;
            }


            // Loads newly copied version of animation and edits it
            AnimationClip curAnim = (AnimationClip)AssetDatabase.LoadAssetAtPath(animPath, typeof(AnimationClip));
            if (curAnim)
                newClips[i] = curAnim;

            // Adds animation information to the animation debug string
            debug += "Loaded animation " + curAnim.name + ". bindings total: " + AnimationUtility.GetCurveBindings(curAnim).Length + '\n';
            
            foreach (var binding in AnimationUtility.GetCurveBindings(curAnim))
            {
                EditorCurveBinding newBinding = new EditorCurveBinding();
                string newPath = "";

                // Skips over the animator component of animations
                if (binding.type == typeof(Animator))
                    continue;

                // In the event of a shapekey on one of the OA meshes
                if (binding.type == typeof(SkinnedMeshRenderer))
                {
                    for (int j = 0; j < OA_Meshes.Length; j++)
                    {
                        if (binding.path == OA_Meshes[j].name)
                        {
                            newPath = FindPathTo(FA_Meshes[j]);
                            newBinding.path = newPath;
                            break;
                        }
                    }
                } 
                else
                {
                    // Generates the new path of the new binding
                    string tempStr = FindPathTo(freezeRoot);
                    newPath = tempStr + "/" + binding.path;
                    newBinding.path = newPath;
                }

                // Sets up the curve of the current event
                var curve = AnimationUtility.GetEditorCurve(curAnim, binding);
                AnimationCurve newCurve = new AnimationCurve();
                newCurve.preWrapMode = curve.preWrapMode;
                newCurve.postWrapMode = curve.postWrapMode;

                for (int j = 0; j < curve.keys.Length; j++)
                {
                    // Sets up the curve keyframe by keyframe
                    newCurve.AddKey(curve.keys[j]);
                }
                curAnim.SetCurve(newPath, binding.type, binding.propertyName, newCurve);
            }

        }
        DEBUG_LOG(debug);

        // Gets the current animator and duplicates it
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(animator.GetInstanceID(), out guid, out localID);
        string animatorPath = "Assets/Freezify/Animators/freeze_" + animator.name + ".controller";

        // Creates a copy of the animator in a new folder, if failed, aborts operation
        if (!AssetDatabase.CopyAsset(AssetDatabase.GUIDToAssetPath(guid), animatorPath))
        {
            DEBUG_LOG(animator.name + ".controller Copy Failed! Aborting...");
            return ErrorCodes.animatorCopyError;
        }


        // Loads newly copied version of animation and edits it
        AnimatorController newAnimator = (AnimatorController)AssetDatabase.LoadAssetAtPath(animatorPath, typeof(AnimatorController));
        foreach (var layer in newAnimator.layers)
        {
            foreach (var state in layer.stateMachine.states)
            {
                if (state.state.motion == null)
                    continue;


                if (state.state.motion.GetType() == typeof(BlendTree))
                {
                    // Makes a copy of the current blend tree state
                    BlendTree newTree = new BlendTree();
                    newTree = (BlendTree) state.state.motion;
                    
                    // Goes through the children and assigns the newly made motions
                    for (int i = 0; i < newTree.children.Length; i++)
                    {
                        // Searches the newClips array for the motion
                        for (int j = 0; j < newClips.Length; j++)
                        {
                            if (newClips[j].name == "freeze_" + newTree.children[i].motion.name)
                            {
                                newTree.AddChild(newClips[j]);
                                break;
                            }
                        }
                    }

                    // Removes original animations
                    for (int i = 0; i <= (newTree.children.Length / 2); i++)
                        newTree.RemoveChild(0);

                    // Adds the motion to the animator
                    newAnimator.SetStateEffectiveMotion(state.state, newTree);
                    continue;
                }

                // Searches the list of animation clips created
                AnimationClip newAnimClip = new AnimationClip();
                for (int i = 0; i < newClips.Length; i++)
                {
                    if (newClips[i].name == "freeze_" + state.state.motion.name)
                    {
                        newAnimClip = newClips[i];
                        break;
                    }
                }
                
                // Replaces it
                newAnimator.SetStateEffectiveMotion(state.state, newAnimClip);
            }
        }

        return ErrorCodes.none;
    }

    // Creates the freeze animation that allows the player to freeze in game
    private void CreateFreezeAnimation()
    {
        if (!Directory.Exists("Assets/Freezify/Animations/Freeze"))
            AssetDatabase.CreateFolder("Assets/Freezify/Animations", "Freeze");

        GameObject[] faarmature = BoneArraySetup(FA_Armature);

        // Creates ON and OFF animations
        for (int i = 0; i < 2; i++)
        {
            AnimationClip freeze = new AnimationClip();
            string path = "";
            Type type = null;
            string propertyName = "";

            // Sets Animation Name
            if (i == 0)
                freeze.name = "Freeze ON";
            if (i == 1)
                freeze.name = "Freeze OFF";

            // Goes through contraints and animates them
            for (int j = 0; j < faarmature.Length; j++)
            {
                AnimationCurve curve = new AnimationCurve();

                // Gets the constraint type
                var rc = faarmature[j].GetComponent<RotationConstraint>();
                var pc = faarmature[j].GetComponent<ParentConstraint>();

                if (rc)
                    type = typeof(RotationConstraint);
                else if (pc)
                    type = typeof(ParentConstraint);


                // Gets path
                path = FindPathTo(faarmature[j]);
                propertyName = "m_Enabled";

                Keyframe key = new Keyframe();
                key.value = i;
                key.inTangent = i;
                key.outTangent = i;

                // Adds the same key twice
                curve.AddKey(key);
                curve.AddKey(key);

                freeze.SetCurve(path, type, propertyName, curve);
            }

            // Goes through original meshes and animates them
            for (int j = 0; j < OA_Meshes.Length; j++)
            {
                AnimationCurve curve = new AnimationCurve();

                // Since these are on the root, path is just the name
                path = OA_Meshes[j].name;
                type = typeof(GameObject);
                propertyName = "m_IsActive";

                Keyframe key = new Keyframe();
                key.value = i;
                key.inTangent = i;
                key.outTangent = i;

                curve.AddKey(key);
                curve.AddKey(key);
                freeze.SetCurve(path, type, propertyName, curve);
            }

            // Goes through active meshes and animates them
            for (int j = 0; j < FA_Meshes.Length; j++)
            {
                AnimationCurve curve = new AnimationCurve();

                // Since these are on the root, path is just the name
                path = FindPathTo(FA_Meshes[j]);
                type = typeof(GameObject);
                propertyName = "m_IsActive";

                // Inverts i
                Keyframe key = new Keyframe();
                key.value = 1 - i;
                key.inTangent = 1 - i;
                key.outTangent = 1 - i;

                curve.AddKey(key);
                curve.AddKey(key);
                freeze.SetCurve(path, type, propertyName, curve);
            }

            // Adds container property
            path = FindPathTo(freezeRoot);
            path = path.TrimEnd(freezeRoot.name.ToCharArray());
            path = path.TrimEnd('/');
            propertyName = "m_Enabled";
            type = typeof(ParentConstraint);

            AnimationCurve _curve = new AnimationCurve();
            Keyframe _key = new Keyframe();
            _key.value = i;
            _key.inTangent = i;
            _key.outTangent = i;

            // Adds the same key twice
            _curve.AddKey(_key);
            _curve.AddKey(_key);
            freeze.SetCurve(path, type, propertyName, _curve);

            // Creates the new animation
            AssetDatabase.CreateAsset(freeze, "Assets/Freezify/Animations/Freeze/" + freeze.name + ".anim");
        }
    }

    // Finds the path to obj, used for animations
    // Params:
    //  obj - The object to find the path to
    // Returns:
    //  the path as a string
    private string FindPathTo(GameObject obj)
    {
        return AnimationUtility.CalculateTransformPath(obj.transform, obj.transform.root);
    }

    // Deletes all ParentConstraint and RotationConstraint objects off of the FA armature
    private void AvatarReset()
    {
        GameObject[] fa = BoneArraySetup(FA_Armature);

        for (int i = 0; i < fa.Length; i++)
        {
            var rc = fa[i].GetComponent<RotationConstraint>();
            if (rc)
                DestroyImmediate(rc, true);

            var pc = fa[i].GetComponent<ParentConstraint>();
            if (pc)
                DestroyImmediate(pc, true);
        }
    }

    // Sets up the bone array given the root of the armature
    // Params:
    //  armature = the root of the armature
    // Returns:
    //  A GameObject array of all children of armature
    private GameObject[] BoneArraySetup(GameObject armature)
    {
        if (!armature) return null;

        Transform[] transforms = armature.GetComponentsInChildren<Transform>(true);
        GameObject[] ret = new GameObject[transforms.Length];

        for (int i = 0; i < transforms.Length; i++)
            ret[i] = transforms[i].gameObject;

        return ret;
    }

    // Performs a null check on the pre-existing mesh arrays
    // Returns:
    //  The error code if any meshes are null. "ErrorCodes.none" if no nulls are found
    private ErrorCodes NullCheck()
    {
        ErrorCodes retVal = ErrorCodes.none;
        bool oa = false, fa = false;

        // Scans through the mesh arrays
        for (int i = 0; i < meshCount; i++)
        {
            if (OA_Meshes[i] == null)
            {
                retVal = ErrorCodes.oaNULL;
                oa = true;
            }

            if (FA_Meshes[i] == null)
            {
                retVal = ErrorCodes.faNULL;
                fa = true;
            }
        }

        // If both the FA and OA Mesh arrays have a null object
        // Returns both null. If not, returns the respective null error
        if (fa && oa)
            retVal = ErrorCodes.bothNULL;

        return retVal;
    }

    // Parses a given error code and prints a LogError message
    // Params:
    //  err = The error code to parse
    private void ErrorParse(ErrorCodes err)
    {
        if (err == ErrorCodes.none)
            return;

        // Nasty switch statement, here are all of the error codes to return
        switch (err)
        {
            case ErrorCodes.animatorClipsMissing:
                Debug.LogWarning("[Freezify] Animator missing animation clips! Skipping...");
                break;
            case ErrorCodes.animationCopyError:
                Debug.LogError("[Freezify] Error! Animation Clip copy failed! Aborting...");
                break;
            case ErrorCodes.animatorCopyError:
                Debug.LogError("[Freezify] Error! Animator Controller copy failed! Aborting...");
                break;
            case ErrorCodes.oaNULL:
                Debug.LogError("[Freezify] Error! NULL object found in Original Avatar Meshes! Aborting...");
                break;
            case ErrorCodes.faNULL:
                Debug.LogError("[Freezify] Error! NULL object found in Freeze Avatar Meshes! Aborting...");
                break;
            case ErrorCodes.bothNULL:
                Debug.LogError("[Freezify] Error! NULL object found in Both Original & Freeze Avatar Meshes! Aborting...");
                break;
            case ErrorCodes.oaArmNULL:
                Debug.LogError("[Freezify] Error! Original Armature object either missing or null! Aborting...");
                break;
            case ErrorCodes.faArmNULL:
                Debug.LogError("[Freezify] Error! Freeze Armature object either missing or null! Aborting...");
                break;
        }
    }

    // Logs a debug string if debug is enabled by user
    // Params:
    //  str = the debug string to send to the console
    private void DEBUG_LOG(string str)
    {
        if (!debug)
            return;

        Debug.Log("[Freezify] [Debug]: " + str);
    }

    public void OnInspectorUpdate()
    {
        Repaint();
    }
}
