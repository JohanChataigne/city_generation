using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;

public class VoronoiDemo : MonoBehaviour
{
	/* Scene objects and other parameters */
    public Material land;
	public GameObject road;
	public GameObject building;
	public GameObject skyscraper;
	public GameObject house;
	public NavMeshSurface surface;

	/* Environment and objects measures */

	public const int PLANE_SIZE = 20;

	/* edges of the plane, in order to center it in (0,0) */
	public const int PLANE_MIN_BORDER = -PLANE_SIZE/2; 
	public const int PLANE_MAX_BORDER = PLANE_SIZE/2;

    public const int NPOINTS = 10;
    public const int SIZE = 1000;
	public const float HOUSE_THRESHOLD = 0.85f;
	public const float BUILDING_THRESHOLD = 0.97f;
	public const float BUILDING_SIZE = 0.05f;
	public const float ROAD_WIDTH = 0.1f * PLANE_SIZE/10;
	public const float ROAD_HEIGHT = 0.001f;
	public const float ROAD_LENGTH = 0.5f * PLANE_SIZE/10;
	public const int CENTER_RADIUS = 200;

	/* Voronoi and heatmap parameters */
	public float freqx = 0.02f, freqy = 0.018f, offsetx = 0.43f, offsety = 0.22f;
    private List<Vector2> m_points;
	private List<LineSegment> m_edges = null;
	private List<LineSegment> m_spanningTree;
	private List<LineSegment> m_delaunayTriangulation;
	private Texture2D tx;

	private float [,] createMap() 
    {
		Vector2 center = new Vector2(SIZE/2, SIZE/2);
        float [,] map = new float[SIZE, SIZE];
        for (int i = 0; i < SIZE; i++)
            for (int j = 0; j < SIZE; j++) {
				Vector2 position = new Vector2(i, j);
				float distance_to_center = (center - position).magnitude / SIZE;
                map[i, j] = Mathf.PerlinNoise(freqx * i + offsetx, freqy * j + offsety) * distance_to_center + (1-distance_to_center);
			}
        return map;
    }

	/* Create list of points from heigthmap with occurences scaling with density of the same point */
	private List<float[]> weightenMap(float[,] map) {

		List<float[]> weighten_map = new List<float[]>();

		for (int i = 0; i < SIZE; i++) {
            for (int j = 0; j < SIZE; j++) {
				
				int x = i - SIZE/2;
				int y = j - SIZE/2;

				if (x*x + y*y > CENTER_RADIUS*CENTER_RADIUS) {
					int occ = (int) (100 * map[i, j]);

					for (int k = 0 ; k < occ ; k++) {
						weighten_map.Add(new float[] {i, j});
					}
				}
				
			}
		}

		return weighten_map;
	}

	/* Instanciate the right type of building (GameObject) depending on the density at given position*/
	private void createBuildings(float x, float y, float angle, float [,] map) {

		if (x >= SIZE || x < 0 || y >= SIZE || y < 0)
			return;

		float density = map[(int)x, (int)y];

		if (density < HOUSE_THRESHOLD) {
			float height = BUILDING_SIZE;
			GameObject h = Instantiate(house, new Vector3(Resize(y), height/2, Resize(x)), Quaternion.Euler(0, angle, 0));
			h.transform.localScale = new Vector3(BUILDING_SIZE, height, BUILDING_SIZE);
		}
		else if (density < BUILDING_THRESHOLD) {
			float height = density/4 + Random.Range(0.0f, BUILDING_SIZE * 2);
			GameObject b = Instantiate(building, new Vector3(Resize(y), height/2, Resize(x)), Quaternion.Euler(0, angle, 0));
			b.transform.localScale = new Vector3(BUILDING_SIZE, height, BUILDING_SIZE);
		}
		else {
			float height = density/4 + Random.Range(0.0f, BUILDING_SIZE * 3);
			GameObject s = Instantiate(skyscraper, new Vector3(Resize(y), height, Resize(x)), Quaternion.Euler(0, angle, 0));
			s.transform.localScale = new Vector3(BUILDING_SIZE, height, BUILDING_SIZE);
		}
	}

	private float Resize(float x) {
		return x / SIZE * PLANE_SIZE + PLANE_MIN_BORDER;
	}

	private Vector2 Resize(Vector2 v) {
		return v / SIZE * PLANE_SIZE;
	}


	void Start ()
	{
        float [,] map=createMap();
		List<float[]> weighten_map = weightenMap(map);
        Color[] pixels = createPixelMap(map);

		/* Change position of plane according to desired borders */
		this.gameObject.transform.position = new Vector3((PLANE_MIN_BORDER+PLANE_MAX_BORDER)/2, 0, (PLANE_MIN_BORDER+PLANE_MAX_BORDER)/2);
		/* Rescale plane with desired size */
		this.gameObject.transform.localScale = new Vector3(PLANE_SIZE/10, 1, PLANE_SIZE/10);

		m_points = new List<Vector2> ();
		List<uint> colors = new List<uint> ();

		/* Create circle points */

		/* center */
		colors.Add((uint)0);
		m_points.Add(new Vector2(SIZE/2, SIZE/2));

		/* circle */
		for (int i = 0; i < SIZE; i++) {
			for (int j = 0; j < SIZE; j++) {

				int x = i - SIZE/2;
				int y = j - SIZE/2;

				if (x*x + y*y == CENTER_RADIUS*CENTER_RADIUS) {
					colors.Add ((uint)0);
					Vector2 vec = new Vector2(i, j); 
					m_points.Add (vec);
				}
			}
		}

        /* Create random points */
		for (int i = 0; i < NPOINTS; i++) {
			colors.Add ((uint)0);
			float[] rnd_coord = weighten_map[(int) Random.Range(0, weighten_map.Count)];
			Vector2 vec = new Vector2(rnd_coord[0], rnd_coord[1]); 
			m_points.Add (vec);
		}

		/* Generate Graphs */
		Delaunay.Voronoi v = new Delaunay.Voronoi (m_points, colors, new Rect (0, 0, SIZE, SIZE));
		m_edges = v.VoronoiDiagram ();
		m_spanningTree = v.SpanningTree (KruskalType.MINIMUM);
		m_delaunayTriangulation = v.DelaunayTriangulation ();
		

		/* Shows Voronoi diagram */
		Color color = Color.blue;
		for (int i = 0; i < m_edges.Count; i++) {
			LineSegment seg = m_edges [i];				
			Vector2 start = (Vector2)seg.p0;
			Vector2 end = (Vector2)seg.p1;
			Vector2 segment = end - start;
			Vector2 resized_segment = Resize(segment);
			float angle = Vector2.SignedAngle(Vector2.right, resized_segment);
			float y_pos = Resize(start.y + segment.y / 2);
			float x_pos = Resize(start.x + segment.x / 2);
			GameObject go = Instantiate(road, new Vector3(y_pos, 0, x_pos), Quaternion.Euler(0, angle+90, 0));
			go.transform.localScale = new Vector3(resized_segment.magnitude * ROAD_LENGTH, ROAD_HEIGHT, ROAD_WIDTH);
			/*DrawLine (pixels, left, right, color);*/
		}

		/* Create buildings */

		/* Create buildings next to roads */
		for (int i = 0; i < m_edges.Count; i++) {
			LineSegment seg = m_edges [i];	
			Vector2 start = (Vector2)seg.p0;
			Vector2 end = (Vector2)seg.p1;

			/* Compute angle of the road */
			Vector2 segment = end - start;
			Vector2 resized_segment = Resize(segment);
			float road_angle = Vector2.SignedAngle(Vector2.right, resized_segment);

			int nb_building = (int)resized_segment.magnitude;

			Vector2 ortho = Vector2.Perpendicular(resized_segment);
			ortho.Normalize();

			for (int k = 1; k < nb_building ; k++) {

				float x = start.x + segment.x * (k * 1.0f/nb_building);
				float y = start.y + segment.y * (k * 1.0f/nb_building);
				createBuildings(x + 10*ortho.x, y + 10*ortho.y, road_angle, map);
				createBuildings(x + -10*ortho.x, y + -10*ortho.y, road_angle, map);
			}	

		}

		/* build navmesh */
		surface.BuildNavMesh();

		/* Shows Delaunay triangulation */
		/*
 		color = Color.red;
		if (m_delaunayTriangulation != null) {
			for (int i = 0; i < m_delaunayTriangulation.Count; i++) {
					LineSegment seg = m_delaunayTriangulation [i];				
					Vector2 left = (Vector2)seg.p0;
					Vector2 right = (Vector2)seg.p1;
					DrawLine (pixels,left, right,color);
			}
		}*/

		/* Shows spanning tree */
		/*
		color = Color.black;
		if (m_spanningTree != null) {
			for (int i = 0; i< m_spanningTree.Count; i++) {
				LineSegment seg = m_spanningTree [i];				
				Vector2 left = (Vector2)seg.p0;
				Vector2 right = (Vector2)seg.p1;
				DrawLine (pixels,left, right,color);
			}
		}*/

		/* Apply pixels to texture */
		/*tx = new Texture2D(WIDTH, HEIGHT);
        land.SetTexture ("_MainTex", tx);
		tx.SetPixels (pixels);
		tx.Apply ();*/

	}


    /* Functions to create and draw on a pixel array */
    private Color[] createPixelMap(float[,] map)
    {
        Color[] pixels = new Color[SIZE * SIZE];
        for (int i = 0; i < SIZE; i++)
            for (int j = 0; j < SIZE; j++)
            {
                pixels[i * SIZE + j] = Color.Lerp(Color.white, Color.black, map[i, j]);
            }
        return pixels;
    }

    private void DrawPoint (Color [] pixels, Vector2 p, Color c) {
		if (p.x<SIZE&&p.x>=0&&p.y<SIZE&&p.y>=0) 
		    pixels[(int)p.x*SIZE+(int)p.y]=c;
	}

	// Bresenham line algorithm
	private void DrawLine(Color [] pixels, Vector2 p0, Vector2 p1, Color c) {
		int x0 = (int)p0.x;
		int y0 = (int)p0.y;
		int x1 = (int)p1.x;
		int y1 = (int)p1.y;

		int dx = Mathf.Abs(x1-x0);
		int dy = Mathf.Abs(y1-y0);
		int sx = x0 < x1 ? 1 : -1;
		int sy = y0 < y1 ? 1 : -1;
		int err = dx-dy;
		while (true) {
            if (x0>=0&&x0<SIZE&&y0>=0&&y0<SIZE)
    			pixels[x0*SIZE+y0]=c;

			if (x0 == x1 && y0 == y1) break;
			int e2 = 2*err;
			if (e2 > -dy) {
				err -= dy;
				x0 += sx;
			}
			if (e2 < dx) {
				err += dx;
				y0 += sy;
			}
		}
	}
}