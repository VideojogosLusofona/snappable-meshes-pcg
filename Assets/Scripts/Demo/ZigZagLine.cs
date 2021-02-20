using UnityEngine;

namespace SnapMeshPCG.Demo
{

    [RequireComponent(typeof(LineRenderer))]
    public class ZigZagLine : MonoBehaviour
    {
        [SerializeField] private int points = 0;

        [SerializeField] private float pointVariation = 0.0f;
        [SerializeField] private float  lineLength = 0.0f;

        private float widthFactor = 0.0f;
        private float widthCurrentVal = 0.0f;
        [SerializeField] private float witdthMaxVal = 0.0f;
        private Vector3 _start;
        private Vector3 _end;
        LineRenderer lineRenderer;

        private void Start() {
            lineRenderer = GetComponent<LineRenderer>();
            if(lineRenderer)
                lineRenderer.enabled = false;
        }
        public void DrawLine(Vector3 endPoint)
        {

            if(lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();
            if(!lineRenderer) return;
            
            lineRenderer.enabled = false;
            _end = endPoint;
            _start = _end;
            _start.y += lineLength;
            float division = lineLength/points;

            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.positionCount = points;
            Vector3[] positions = new Vector3[points];
            for (int i = 0; i < positions.Length; i++)
            {
                float variation = Random.Range(-pointVariation, pointVariation);
                positions[i] = new Vector3(_start.x + variation, _start.y - division * i, _start.z + variation);

                if(i == positions.Length - 1)
                    positions[i] = new Vector3(_end.x, _end.y, _end.z);
                
            }
            lineRenderer.SetPositions(positions);
            widthFactor = widthCurrentVal / witdthMaxVal;
            lineRenderer.enabled = true;

        }


    }
}
