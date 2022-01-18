using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorLidarProjection : MonoBehaviour
{
    public float squareSize = 0.5f;
    public float lineThickness = 0.05f;
    public float colorGradientDifference = 25;
    public float xInterval = 10f;
    public float yInterval = 10f;
    public float yRange = 110f;
    public float xRange = 110f;
    public float detectionDistance = 200f;
    public Material baseMaterial;
    List<Material> materials;
    List<List<Vector3>> forwards;
    List<List<Vector3>> distanceEvaluation;
    List<List<LineRenderer>> lineRenderers;


    private void Start()
    {
        forwards = new List<List<Vector3>>();
        distanceEvaluation = new List<List<Vector3>>();
        lineRenderers = new List<List<LineRenderer>>();
        materials = new List<Material>();
        MakeGradient();
    }

    private void Update()
    {
        DirectionRaycast();
        EvaluateDistance();
        CreateBoxes();
    }

    public void MakeBoxOnPoint(Quaternion rotation, Vector3 point, LineRenderer renderer, Material mat, float squareSize = 1f, float lineThickness = 0.1f)
    {
        Vector3[] box = new Vector3[] { Vector3.zero, Vector3.right, Vector3.right + Vector3.up, Vector3.up, Vector3.zero, Vector3.right + Vector3.up, Vector3.right, Vector3.up };
        for (int i = 0; i < box.Length; i++)
        {
            box[i] -= new Vector3(0.5f, 0.5f);
            box[i] *= squareSize;
            box[i] = rotation * box[i];
            box[i] += point;
        }

        renderer.positionCount = box.Length;
        renderer.SetPositions(box);
        renderer.startWidth = lineThickness;
        renderer.endWidth = lineThickness;
        renderer.material = mat;
    }

    public void MakeGradient()
    {
        for (int i = 0; i < colorGradientDifference; i++)
        {
            float per = (float)i / (float)colorGradientDifference;
            var mat = new Material(baseMaterial);
            mat.SetColor("_Color", new Color(per, 1f - per, 0));
            materials.Add(mat);
        }
    }

    public int GetGradientIndex(float zeroToOne)
    {
        if (zeroToOne == 0)
        {
            return 0;
        }
        float floatIndex = zeroToOne * (colorGradientDifference - 2);
        int index = Mathf.CeilToInt(floatIndex);
        return index;
    }

    public void DirectionRaycast()
    {
        var yStart = -yRange / 2;
        var yEnd = yRange / 2;
        var xStart = -xRange / 2;
        var xEnd = xRange / 2;
        int indexY = 0;
        for (float currY = yStart; currY < yEnd; currY += yInterval, indexY++)
        {
            if (forwards.Count <= indexY)
            {
                forwards.Add(new List<Vector3>());
            }

            int indexX = 0;
            for (float currX = xStart; currX < xEnd; currX += xInterval, indexX++)
            {
                if (forwards[indexY].Count <= indexX)
                {
                    forwards[indexY].Add(Vector3.zero);
                }

                Vector3 forward = Vector3.forward;
                forward = Quaternion.Euler(currX, currY, 0) * forward;
                forwards[indexY][indexX] = forward;

                Debug.DrawRay(transform.position, forward, Color.red);
            }
        }
    }

    public void EvaluateDistance()
    {
        for (int y = 0; y < forwards.Count; y++)
        {
            if (distanceEvaluation.Count <= y)
            {
                distanceEvaluation.Add(new List<Vector3>());
            }
            for (int x = 0; x < forwards[y].Count; x++)
            {
                if (distanceEvaluation[y].Count <= x)
                {
                    distanceEvaluation[y].Add(Vector3.zero);
                }

                var fwd = forwards[y][x];
                RaycastHit hit;
                if (Physics.Raycast(transform.position, fwd, out hit, detectionDistance))
                {
                    distanceEvaluation[y][x] = hit.point;
                } else
                {
                    distanceEvaluation[y][x] = fwd * detectionDistance;
                }
            }
        }
    }

    public void CreateBoxes()
    {
        for (int y = 0; y < distanceEvaluation.Count; y++)
        {
            if (lineRenderers.Count <= y)
            {
                lineRenderers.Add(new List<LineRenderer>());
            }
            for (int x = 0; x < distanceEvaluation[y].Count; x++)
            {
                if (lineRenderers[y].Count <= x)
                {
                    GameObject gm = new GameObject("Renderer " + x + " " + y);
                    gm.transform.parent = transform;
                    LineRenderer ln = gm.AddComponent<LineRenderer>();
                    lineRenderers[y].Add(ln);
                }

                var res = GetGradientIndex(distanceEvaluation[y][x].magnitude / detectionDistance);
                Material mat = materials[res];
                MakeBoxOnPoint(transform.rotation, distanceEvaluation[y][x], lineRenderers[y][x], mat, squareSize, lineThickness);
            }
        }
    }

}
