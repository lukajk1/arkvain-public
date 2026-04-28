using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to make ragdoll joints stiffer.
/// Select a GameObject with ragdoll joints and use the context menu or inspector button.
/// </summary>
public class RagdollJointStiffener : EditorWindow
{
    [Header("Spring Settings")]
    [SerializeField] private float springValue = 5000f;
    [SerializeField] private float damperValue = 500f;

    [Header("Rigidbody Settings")]
    [SerializeField] private float angularDrag = 10f;
    [SerializeField] private float drag = 2f;

    [Header("Optionally Reduce Limits")]
    [SerializeField] private bool reduceLimits = false;
    [SerializeField] private float limitMultiplier = 0.7f;

    private GameObject targetObject;

    [MenuItem("Tools/Ragdoll Joint Stiffener")]
    public static void ShowWindow()
    {
        GetWindow<RagdollJointStiffener>("Ragdoll Stiffener");
    }

    private void OnGUI()
    {
        GUILayout.Label("Ragdoll Joint Stiffener", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetObject = (GameObject)EditorGUILayout.ObjectField("Target Ragdoll", targetObject, typeof(GameObject), true);

        EditorGUILayout.Space();
        GUILayout.Label("Spring Settings", EditorStyles.boldLabel);
        springValue = EditorGUILayout.FloatField("Spring Value", springValue);
        damperValue = EditorGUILayout.FloatField("Damper Value", damperValue);

        EditorGUILayout.Space();
        GUILayout.Label("Rigidbody Settings", EditorStyles.boldLabel);
        angularDrag = EditorGUILayout.FloatField("Angular Drag", angularDrag);
        drag = EditorGUILayout.FloatField("Drag", drag);

        EditorGUILayout.Space();
        GUILayout.Label("Angle Limits (Optional)", EditorStyles.boldLabel);
        reduceLimits = EditorGUILayout.Toggle("Reduce Limits", reduceLimits);
        if (reduceLimits)
        {
            limitMultiplier = EditorGUILayout.Slider("Limit Multiplier", limitMultiplier, 0.1f, 1f);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply to All Joints", GUILayout.Height(40)))
        {
            if (targetObject != null)
            {
                ApplyStiffness(targetObject);
            }
            else
            {
                Debug.LogWarning("Please assign a target ragdoll GameObject.");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This will modify all CharacterJoint components on the target GameObject and its children.\n\n" +
            "Recommended values for stiff ragdolls:\n" +
            "• Spring: 5000-10000\n" +
            "• Damper: 500-1000\n" +
            "• Angular Drag: 10-20",
            MessageType.Info);
    }

    private void ApplyStiffness(GameObject target)
    {
        Undo.RegisterFullObjectHierarchyUndo(target, "Stiffen Ragdoll Joints");

        CharacterJoint[] joints = target.GetComponentsInChildren<CharacterJoint>();
        Rigidbody[] rigidbodies = target.GetComponentsInChildren<Rigidbody>();

        if (joints.Length == 0)
        {
            Debug.LogWarning($"No CharacterJoint components found on {target.name} or its children.");
            return;
        }

        // Apply to all joints
        foreach (CharacterJoint joint in joints)
        {
            // Swing Limit Spring (covers both swing axes)
            SoftJointLimitSpring swingSpring = joint.swingLimitSpring;
            swingSpring.spring = springValue;
            swingSpring.damper = damperValue;
            joint.swingLimitSpring = swingSpring;

            // Twist Limit Spring
            SoftJointLimitSpring twistSpring = joint.twistLimitSpring;
            twistSpring.spring = springValue;
            twistSpring.damper = damperValue;
            joint.twistLimitSpring = twistSpring;

            // Optionally reduce angle limits
            if (reduceLimits)
            {
                SoftJointLimit lowTwist = joint.lowTwistLimit;
                lowTwist.limit *= limitMultiplier;
                joint.lowTwistLimit = lowTwist;

                SoftJointLimit highTwist = joint.highTwistLimit;
                highTwist.limit *= limitMultiplier;
                joint.highTwistLimit = highTwist;

                SoftJointLimit swing1 = joint.swing1Limit;
                swing1.limit *= limitMultiplier;
                joint.swing1Limit = swing1;

                SoftJointLimit swing2 = joint.swing2Limit;
                swing2.limit *= limitMultiplier;
                joint.swing2Limit = swing2;
            }

            EditorUtility.SetDirty(joint);
        }

        // Apply rigidbody settings
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.angularDamping = angularDrag;
            rb.linearDamping = drag;
            EditorUtility.SetDirty(rb);
        }

        Debug.Log($"Applied stiffness settings to {joints.Length} joints and {rigidbodies.Length} rigidbodies on {target.name}");
    }

    // Context menu option for quick access
    [MenuItem("GameObject/Ragdoll/Stiffen All Joints", false, 0)]
    private static void StifenJointsContextMenu()
    {
        if (Selection.activeGameObject != null)
        {
            RagdollJointStiffener window = GetWindow<RagdollJointStiffener>("Ragdoll Stiffener");
            window.targetObject = Selection.activeGameObject;
            window.Show();
        }
        else
        {
            Debug.LogWarning("Please select a GameObject with ragdoll joints.");
        }
    }
}
