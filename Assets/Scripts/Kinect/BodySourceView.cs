using UnityEngine;
using System.Collections.Generic;
using Kinect = Windows.Kinect;

public class BodySourceView : MonoBehaviour 
{
    public GameObject BodySourceManager;
    public Transform parentTransform;

    private Material jointMaterial, boneMaterial;
    
    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private BodySourceManager _BodyManager;

    private const string JOINT_ID_FORMAT = "joint:{0}";
    private const string BONE_ID_FORMAT = "bone:{0}-{1}";

    private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },
        
        { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },
        
        //{ Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
        //{ Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        //{ Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },
        
        //{ Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
        //{ Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        //{ Kinect.JointType.HandRight, Kinect.JointType.WristRight },
        { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },
        
        { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
        { Kinect.JointType.Neck, Kinect.JointType.Head },
    };

    void Start()
    {
        jointMaterial = Resources.Load("Materials/JointMaterial") as Material;
        boneMaterial = Resources.Load("Materials/boneMaterial") as Material;
    }

    void Update () 
    {
        if (BodySourceManager == null)
        {
            return;
        }
        
        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_BodyManager == null)
        {
            return;
        }
        
        Kinect.Body[] data = _BodyManager.GetData();
        if (data == null)
        {
            return;
        }
        
        List<ulong> trackedIds = new List<ulong>();
        foreach(var body in data)
        {
            if (body == null)
            {
                continue;
            }
                
            if(body.IsTracked)
            {
                trackedIds.Add (body.TrackingId);
            }
        }
        
        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);
        
        // First delete untracked bodies
        foreach(ulong trackingId in knownIds)
        {
            if(!trackedIds.Contains(trackingId))
            {
                Destroy(_Bodies[trackingId]);
                _Bodies.Remove(trackingId);
            }
        }

        foreach(var body in data)
        {
            if (body == null)
            {
                continue;
            }
            
            if(body.IsTracked)
            {
                if(!_Bodies.ContainsKey(body.TrackingId))
                {
                    _Bodies[body.TrackingId] = CreateBodyObject(body, body.TrackingId);
                }
                RefreshBodyObject(body, _Bodies[body.TrackingId]);
            }
        }
    }

    private bool FilterJoint(Kinect.JointType jt)
    {
        if (jt == Kinect.JointType.ThumbLeft | jt == Kinect.JointType.ThumbRight |
            jt == Kinect.JointType.HandTipLeft | jt == Kinect.JointType.HandTipRight |
            jt == Kinect.JointType.HandLeft | jt == Kinect.JointType.HandRight)
            return true;
        return false;
    }

    private GameObject ConstructJoint()
    {
        GameObject ob = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        if (ob.GetComponent<SphereCollider>() != null)
            Destroy(ob.GetComponent<SphereCollider>());

        // fake (but clever) 2d physics
        ob.AddComponent<CapsuleCollider>().height = 1000.0f;

        // always render on top
        //ob.layer = LayerMask.NameToLayer("KinectBody");
        ob.GetComponent<Renderer>().material = jointMaterial;

        return ob;
    }

    private GameObject ConstructBone(Kinect.Joint source, Kinect.Joint dest)
    {
        // special cylinder instantiated between two points
        GameObject ob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        if (ob.GetComponent<Collider>() != null)
            Destroy(ob.GetComponent<Collider>());

        // fake (but clever) 2d physics
        GameObject child = new GameObject("collider", typeof(CapsuleCollider));
        child.transform.SetParent(ob.transform);

        ob = UpdateBone(ob, source, dest);

        // always render on top
        //ob.layer = LayerMask.NameToLayer("KinectBody");
        ob.GetComponent<Renderer>().material = boneMaterial;

        return ob;
    }

    private GameObject UpdateBone(GameObject ob, Kinect.Joint sourceJoint, Kinect.Joint destJoint)
    {
        Vector3 src = GetVector3FromJoint(sourceJoint);
        Vector3 dst = GetVector3FromJoint(destJoint);
        Vector3 offset = dst - src;
        Vector3 pos = src + offset*0.5f;

        ob.transform.position = pos;
        ob.transform.LookAt(src);

        Vector3 localScale = ob.transform.localScale;
        localScale.y = offset.magnitude/2;
        ob.transform.localScale = localScale;

        // fix capsule orientation
        Quaternion rot = new Quaternion { eulerAngles = new Vector3(90.0f, 0.0f, 0.0f) };
        ob.transform.localRotation *= rot;

        // new plan: instantiate a number of sphere colliders between two points and multiply their height
        return ob;
    }

    private GameObject CreateBodyObject(Kinect.Body body, ulong id)
    {
        GameObject bodyObj = new GameObject("body:" + id);
        bodyObj.transform.SetParent(parentTransform);

        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            if (FilterJoint(jt)) continue;

            // joints
            GameObject jointObj = ConstructJoint();

            jointObj.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            jointObj.name = string.Format(JOINT_ID_FORMAT, jt.ToString());
            jointObj.transform.SetParent(bodyObj.transform);

            // bones
            if (_BoneMap.ContainsKey(jt))
            {
                Kinect.Joint destJoint = body.Joints[_BoneMap[jt]];
                GameObject boneObj = ConstructBone(body.Joints[jt], destJoint);

                string boneId = string.Format(BONE_ID_FORMAT, jt, _BoneMap[jt]);
                boneObj.name = boneId;
                boneObj.transform.SetParent(bodyObj.transform);
            }
        }
        return bodyObj;
    }
    
    private void RefreshBodyObject(Kinect.Body body, GameObject bodyObject)
    {
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            if (FilterJoint(jt)) continue;

            Kinect.Joint sourceJoint = body.Joints[jt];
            Kinect.Joint? nonNullabledestJoint = null;
            Kinect.Joint destJoint = nonNullabledestJoint ?? new Kinect.Joint();

            if (_BoneMap.ContainsKey(jt))
            {
                destJoint = body.Joints[_BoneMap[jt]];
            }

            Transform jointObj = bodyObject.transform.Find(string.Format(JOINT_ID_FORMAT, jt.ToString()));
            jointObj.localPosition = GetVector3FromJoint(sourceJoint);

            if (_BoneMap.ContainsKey(jt))
            {
                string boneId = string.Format(BONE_ID_FORMAT, jt, _BoneMap[jt]);
                Transform boneObj = bodyObject.transform.Find(boneId);
                UpdateBone(boneObj.gameObject, sourceJoint, destJoint);
            }
        }
    }
    
    private static Color GetColorForState(Kinect.TrackingState state)
    {
        switch (state)
        {
        case Kinect.TrackingState.Tracked:
            return Color.green;

        case Kinect.TrackingState.Inferred:
            return Color.red;

        default:
            return Color.black;
        }
    }
    
    private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        return new Vector3(joint.Position.X * 10, joint.Position.Y * 10, joint.Position.Z * 10);
    }
}
