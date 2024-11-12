using System.Collections.Generic;
using UnityEngine;

public class TestPoint : MonoBehaviour
{
    [SerializeField]
    private List<TestPoint> links;

    public int      group = 0;
    public float    mass = 1;
    public bool     debugRender = true;
    public bool     locked = false;

    public void ResetLinks()
    {
        links = new List<TestPoint>();
    }

    internal void AddLink(TestPoint nextPoint)
    {
        if (links == null) ResetLinks();
        links.Add(nextPoint);
    }

    internal List<TestPoint> GetLinks() => links;

    public static readonly Color[] Colors = new Color[]
    {
        Color.green,
        Color.red,
        Color.cyan,
        Color.yellow,
        Color.magenta,
        Color.blue,
    };

    private void OnDrawGizmos()
    {
        if (!debugRender) return;

        float sizePerMass = Mathf.Log(mass + 1.0f);

        Gizmos.color = Colors[group].ChangeAlpha(0.5f);
        Gizmos.DrawSphere(transform.position, 0.25f * sizePerMass);

        if (links != null)
        {
            Gizmos.color = Colors[group];
            foreach (var p in links)
            {
                if (p != null)
                {
                    Gizmos.DrawLine(transform.position, p.transform.position);
                }
            }
        }
    }
}
