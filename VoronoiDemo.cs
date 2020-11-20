using UnityEngine;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;

public class VoronoiDemo : MonoBehaviour
{

    public Material land;
	public GameObject road;
	public GameObject building;
	public GameObject skyscraper;
	public GameObject house;
    public const int NPOINTS = 100;
    public const int WIDTH = 1000;
    public const int HEIGHT = 1000;
	public const float HOUSE_THRESHOLD = 0.85f;
	public const float BUILDING_THRESHOLD = 0.97f;
	public const float BUILDING_SIZE = 0.08f;
	public float freqx = 0.02f, freqy = 0.018f, offsetx = 0.43f, offsety = 0.22f;

    private List<Vector2> m_points;
	private List<LineSegment> m_edges = null;
	private List<LineSegment> m_spanningTree;
	private List<LineSegment> m_delaunayTriangulation;
	private Texture2D tx;

	private float [,] createMap() 
    {
		Vector2 center = new Vector2(WIDTH/2, HEIGHT/2);
        float [,] map = new float[WIDTH, HEIGHT];
        for (int i = 0; i < WIDTH; i++)
            for (int j = 0; j < HEIGHT; j++) {
				Vector2 position = new Vector2(i, j);
				float distance_to_center = (center - position).magnitude/WIDTH;
                map[i, j] = Mathf.PerlinNoise(freqx * i + offsetx, freqy * j + offsety) * distance_to_center + (1-distance_to_center);
			}
        return map;
    }

	/* Create list of points from heigthmap with occurences scaling with density of the same point */
	private List<float[]> weightenMap(float[,] map) {

		List<float[]> weighten_map = new List<float[]>();

		for (int i = 0; i < WIDTH; i++) {
            for (int j = 0; j < HEIGHT; j++) {
				
				int occ = (int) (100 * map[i, j]);

				for (int k = 0 ; k < occ ; k++) {
					weighten_map.Add(new float[] {i, j});
				}
			}
		}

		return weighten_map;
	}

	/* Instanciate the right type of building (GameObject) depending on the density at given position*/
	private void createBuildings(float x, float y, float angle, float [,] map) {

		if (x >= WIDTH || x < 0 || y >= HEIGHT || y < 0)
			return;

		float density = map[(int)x, (int)y];

		if (density < HOUSE_THRESHOLD) {
			GameObject h = Instantiate(house, new Vector3(y / WIDTH * 10 - 5, 0.05f, x / HEIGHT * 10 - 5), Quaternion.Euler(0, angle, 0));
			h.transform.localScale = new Vector3(BUILDING_SIZE, BUILDING_SIZE, BUILDING_SIZE);
		}
		else if (density < BUILDING_THRESHOLD) {
			GameObject b = Instantiate(building, new Vector3(y / WIDTH * 10 - 5, density/4, x / HEIGHT * 10 - 5), Quaternion.Euler(0, angle, 0));
			b.transform.localScale = new Vector3(BUILDING_SIZE, density/2, BUILDING_SIZE);
		}
		else {
			GameObject s = Instantiate(skyscraper, new Vector3(y / WIDTH * 10 - 5, density/2, x / HEIGHT * 10 - 5), Quaternion.Euler(0, angle, 0));
			s.transform.localScale = new Vector3(BUILDING_SIZE, density/2, BUILDING_SIZE);
		}
	}

	void Start ()
	{
        float [,] map=createMap();
		List<float[]> weighten_map = weightenMap(map);
        Color[] pixels = createPixelMap(map);

        /* Create random points points */
		
		m_points = new List<Vector2> ();
		List<uint> colors = new List<uint> ();
		for (int i = 0; i < NPOINTS; i++) {
			colors.Add ((uint)0);
			float[] rnd_coord = weighten_map[(int) Random.Range(0, weighten_map.Count)];
			Vector2 vec = new Vector2(rnd_coord[0], rnd_coord[1]); 
			m_points.Add (vec);
		}

		/* Generate Graphs */
		Delaunay.Voronoi v = new Delaunay.Voronoi (m_points, colors, new Rect (0, 0, WIDTH, HEIGHT));
		m_edges = v.VoronoiDiagram ();
		m_spanningTree = v.SpanningTree (KruskalType.MINIMUM);
		m_delaunayTriangulation = v.DelaunayTriangulation ();
		

		/* Shows Voronoi diagram */
		Color color = Color.blue;
		for (int i = 0; i < m_edges.Count; i++) {
			LineSegment seg = m_edges [i];				
			Vector2 left = (Vector2)seg.p0;
			Vector2 right = (Vector2)seg.p1;
			Vector2 segment = (right - left) / WIDTH * 100;
			float angle = Vector2.SignedAngle(Vector2.right, segment);
			GameObject go = Instantiate(road, new Vector3(left.y / HEIGHT * 10 - 5, 0, left.x / WIDTH * 10 - 5), Quaternion.Euler(0, angle+90, 0));
			go.transform.localScale = new Vector3(segment.magnitude, 1, 1);
			/*DrawLine (pixels, left, right, color);*/
		}

		/* Create buildings */

		/* Create 1 building for each cell in heatmap */
		/*
		for (int i = 0; i < m_points.Count; i++) {

			float x = m_points[i].x;
			float y = m_points[i].y;
			createBuildings(x, y, map);
			
		}*/

		/* Create buildings next to roads */
		for (int i = 0; i < m_edges.Count; i++) {
			LineSegment seg = m_edges [i];	
			Vector2 left = (Vector2)seg.p0;
			Vector2 right = (Vector2)seg.p1;

			/* Compute angle of the road */
			Vector2 segment = (right - left) / WIDTH * 100;
			float road_angle = Vector2.SignedAngle(Vector2.right, segment);

			if(segment.magnitude > 1) {

				int nb_building = (int)segment.magnitude;

				Vector2 ortho = Vector2.Perpendicular(segment);
				ortho.Normalize();

				for (int k = 1; k < nb_building ; k++) {

					float x = left.x + segment.x * WIDTH / 100 * (k * 1.0f/nb_building);
					float y = left.y + segment.y * WIDTH / 100 * (k * 1.0f/nb_building);
					createBuildings(x + 10*ortho.x, y + 10*ortho.y, road_angle, map);
					createBuildings(x + -10*ortho.x, y + -10*ortho.y, road_angle, map);
				}

				//float middle_x = (left.x + right.x) / 2;
				//float middle_y = (left.y + right.y) / 2;
				/* create buildings with road angle */
				//createBuildings(middle_x + ortho.x, middle_y + ortho.y, road_angle, map);
				//createBuildings(middle_x - 10*ortho.x, middle_y - 10*ortho.y, road_angle, map);		
			}
		}

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
        Color[] pixels = new Color[WIDTH * HEIGHT];
        for (int i = 0; i < WIDTH; i++)
            for (int j = 0; j < HEIGHT; j++)
            {
                pixels[i * HEIGHT + j] = Color.Lerp(Color.white, Color.black, map[i, j]);
            }
        return pixels;
    }

    private void DrawPoint (Color [] pixels, Vector2 p, Color c) {
		if (p.x<WIDTH&&p.x>=0&&p.y<HEIGHT&&p.y>=0) 
		    pixels[(int)p.x*HEIGHT+(int)p.y]=c;
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
            if (x0>=0&&x0<WIDTH&&y0>=0&&y0<HEIGHT)
    			pixels[x0*HEIGHT+y0]=c;

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