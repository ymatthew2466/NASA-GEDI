//  OLD VERSION

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class WaveformVisualizer : MonoBehaviour
// {
//     public CSVParser csvParser;  // reference to CSVParser
//     public GameObject barPrefab;  // prefab for visualizing data
//     private float scale = 25;  // scale to place cylinders
//     public float gridCellSize = 10f;  // size of each grid cell
//     private Dictionary<Vector2Int, CSVParser.GEDIDataPoint> gridDataPoints;

//     // Start is called before the first frame update
//     void Start()
//     {
//         if (this.csvParser != null) {
//             // check if CSV is loaded
//             if (csvParser.getDataPoints() == null || csvParser.getDataPoints().Count == 0) {
//                 csvParser.loadCSV();  // force load if null
//             }

//             List<CSVParser.GEDIDataPoint> dataPoints = csvParser.getDataPoints();

//             if (dataPoints != null) {
//                 this.VisualizeData(dataPoints);
//             } 
//             else {
//                 Debug.LogError("dataPoints is null.");
//             }
//         }
//         else {
//             Debug.LogError("CSVParser is not assigned.");
//         }
//     }

//     public void VisualizeData(List<CSVParser.GEDIDataPoint> dataPoints){
//         for (int i=0; i<dataPoints.Count; i+=1){
//             CSVParser.GEDIDataPoint point = dataPoints[i];
//             Vector3 pos = this.LatLong2Unity(point.latitude, point.longitude, point.elevation);
//             // this.CreateBar(pos, point.rh98);

//             float rhLevel = point.rh98;
//             GameObject cyl = this.CreateBar(pos, rhLevel);

//             // map cylinder to a grid pos

//             // this.CreateBar(pos, 100);  // static height to test placement

//             // if(i == 0){
//             //     Debug.Log(pos[0]);
//             //     Debug.Log(pos[1]);
//             //     Debug.Log(pos[2]);
//             // }
//         }
        
//     }

//     private Vector3 LatLong2Unity(float latitude, float longitude, float elevation){
//         float x = longitude * scale;
//         float y = elevation * (1/scale);
//         // float y = 0;  // temporarily keep everything same level
//         float z = latitude * scale;
//         return new Vector3(x, y, z);
//     }

//     private GameObject CreateBar(Vector3 pos, float height){
//         GameObject bar = Instantiate(this.barPrefab, pos, Quaternion.identity);
//         bar.transform.localScale = new Vector3(1, height * 0.1f, 1);  // Scale the bar's height based on the rh98 value
//         return bar;
//     }


//     // Update is called once per frame
//     void Update()
//     {
        
//     }
// }
