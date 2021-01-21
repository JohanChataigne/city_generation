using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Delaunay;
using Delaunay.Geo;
using System;
using System.Linq;


public class buildCity : MonoBehaviour
{
	/* Scene objects and other parameters */
    public Material land;
	public GameObject road;
	public GameObject building;
	public GameObject skyscraper;
	public GameObject house;
	public GameObject agent;
	public NavMeshSurface surface;

	/* Environment and objects measures */

	/* Size of the plane in unity scene */
	private const int PLANE_SIZE = 20;

	/* edges of the plane, in order to center it in (0,0) */
	private const int PLANE_MIN_BORDER = -PLANE_SIZE/2; 
	private const int PLANE_MAX_BORDER = PLANE_SIZE/2;

	/* Number of random points to build voronoi graph */
    public const int NPOINTS = 10;

	/* Size of density map */
    private const int SIZE = 1000;
	private float [,] map;

	/* Buildings density thresholds to determine building type */
	private const float HOUSE_THRESHOLD = 0.90f;
	private const float BUILDING_THRESHOLD = 0.95f;

	/* Buildings dimensions */
	private const float BUILDING_SIZE = 0.1f * PLANE_SIZE/10;
	private const float ROAD_WIDTH = 0.1f * PLANE_SIZE/10;
	private const float ROAD_HEIGHT = 0.01f * PLANE_SIZE/10;
	private const float ROAD_LENGTH = 0.5f * PLANE_SIZE/10;

	/* Buildings lists */
	private List<GameObject> m_houses = new List<GameObject>();
	private List<GameObject> m_buildings = new List<GameObject>();
	private List<GameObject> m_skycrapers = new List<GameObject>();

	private List<GameObject> m_habitations = new List<GameObject>();
	private List<GameObject> m_workplaces = new List<GameObject>();

	/* Agents list and dimensions */
	private List<GameObject> m_agents = new List<GameObject>();
	private List<GameObject> m_homeless = new List<GameObject>();
	private List<GameObject> m_workless = new List<GameObject>();
	private Vector3 AGENT_DIMENSIONS = new Vector3(0.02f, 0.05f, 0.02f);

	/* City center circle radius */
	public const int CENTER_RADIUS = 200;

	/* Voronoi and heatmap parameters */
	public float freqx = 0.02f, freqy = 0.018f, offsetx = 0.43f, offsety = 0.22f;
    private List<Vector2> m_points;
	private List<LineSegment> m_edges = null;
	private List<LineSegment> m_spanningTree;
	private List<LineSegment> m_delaunayTriangulation;
	private Texture2D tx;

	/* Population parameters */
	private int POPULATION_SIZE = 100;

	/* build status */
	private bool done = false;

	/* Time ranges */

	/* Start of the day for agents */
	float early_start = 0.20f;
	float late_start = 0.30f;

	/* End of the day */
	float early_end = 0.70f;
	float late_end = 0.80f;

	private float [,] createMap() 
    {
		Vector2 center = new Vector2(SIZE/2, SIZE/2);
        float [,] map = new float[SIZE, SIZE];
        for (int i = 0; i < SIZE; i++)
            for (int j = 0; j < SIZE; j++) {
				Vector2 position = new Vector2(i, j);
				float distance_to_center = (center - position).magnitude / SIZE;
				/* Higher values when we get closer to the center */
                map[i, j] = Mathf.PerlinNoise(freqx * i + offsetx, freqy * j + offsety) * distance_to_center + (1-distance_to_center);
				if (map[i, j] > 1) map[i, j] = 1;
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
	private void createBuildings(float x, float y, float angle, Vector3 toRoad) {

		if (x >= SIZE || x < 0 || y >= SIZE || y < 0)
			return;

		float density = map[(int)x, (int)y];

		if (density < HOUSE_THRESHOLD) {
			float height = BUILDING_SIZE;
			GameObject h = Instantiate(house, new Vector3(ResizeToScene(y), height/2, ResizeToScene(x)), Quaternion.Euler(0, angle, 0));
			h.transform.localScale = new Vector3(BUILDING_SIZE, height, BUILDING_SIZE);
			h.transform.parent = this.transform;
			h.GetComponent<Building>().Init(toRoad);
			h.GetComponent<Building>().SwitchToHabitation();
			m_houses.Add(h);
		}
		else if (density < BUILDING_THRESHOLD) {
			float height = density + UnityEngine.Random.Range(0.0f, BUILDING_SIZE * 2);
			GameObject b = Instantiate(building, new Vector3(ResizeToScene(y), height/2, ResizeToScene(x)), Quaternion.Euler(0, angle, 0));
			b.transform.localScale = new Vector3(BUILDING_SIZE, height, BUILDING_SIZE);
			b.transform.parent = this.transform;
			b.GetComponent<Building>().Init(toRoad);
			m_buildings.Add(b);
		}
		else {
			float height = density + UnityEngine.Random.Range(BUILDING_SIZE * 3, BUILDING_SIZE * 4);
			GameObject s = Instantiate(skyscraper, new Vector3(ResizeToScene(y), height/2, ResizeToScene(x)), Quaternion.Euler(0, angle, 0));
			s.transform.localScale = new Vector3(BUILDING_SIZE, height, BUILDING_SIZE);
			s.transform.parent = this.transform;
			s.GetComponent<Building>().Init(toRoad);
			m_skycrapers.Add(s);
		}
	}

	private float ResizeToScene(float x) {
		return x / SIZE * PLANE_SIZE + PLANE_MIN_BORDER;
	}

	private Vector2 ResizeToScene(Vector2 v) {
		return v / SIZE * PLANE_SIZE;
	}

	private Vector3 ResizeToScene(Vector3 v) {
		return v / SIZE * PLANE_SIZE;
	}

	private float ResizeToMap(float x) {
		return (x) / PLANE_SIZE * SIZE;
	}

	public List<GameObject> GetAgents() { return m_agents; }

	public void removeBuildingFromList(string type, GameObject b) {

		switch (type)
		{
			case "House":
				m_houses.Remove(b);
				//Debug.Log("removed house");
				break;

			case "Building":
				m_buildings.Remove(b);
				//Debug.Log("removed build");
				break;

			case "Skyscraper":
				m_skycrapers.Remove(b);
				//Debug.Log("removed skys");
				break;

			default:
				Debug.Log(String.Format("Unknown building type: {0}", type));
				break;
		}

		if (b.GetComponent<Building>().IsHabitable()) m_habitations.Remove(b);
		else m_workplaces.Remove(b);
	}

	/* Instatiates roads and buildings */
	private void createStructures() {

		/* Create roads and buildings according to Voronoi Diagram */
		for (int i = 0; i < m_edges.Count; i++) {

			LineSegment seg = m_edges [i];				
			Vector2 start = (Vector2)seg.p0;
			Vector2 end = (Vector2)seg.p1;
			Vector2 segment = end - start;
			Vector2 resized_segment = ResizeToScene(segment);
			
			/* Angle of the road */

			float road_angle = Vector2.SignedAngle(Vector2.right, resized_segment);

			/* Create the road */
			float y_pos = ResizeToScene(start.y + segment.y / 2);
			float x_pos = ResizeToScene(start.x + segment.x / 2);
			GameObject go = Instantiate(road, new Vector3(y_pos, ROAD_HEIGHT/2, x_pos), Quaternion.Euler(0, road_angle+90, 0));
			go.transform.localScale = new Vector3(resized_segment.magnitude * ROAD_LENGTH, ROAD_HEIGHT, ROAD_WIDTH);

			/* Add the buildings */

			int nb_building = (int)(resized_segment.magnitude / (BUILDING_SIZE * 1.5));

			Vector2 ortho = Vector2.Perpendicular(resized_segment);
			ortho.Normalize();

			for (int k = 1; k < nb_building ; k++) {

				float x = start.x + segment.x * (k * 1.0f/nb_building);
				float y = start.y + segment.y * (k * 1.0f/nb_building);

				float spacing_coeff = ResizeToMap(ROAD_WIDTH/2 + 3 * BUILDING_SIZE/4);

				Vector3 toRoad = ResizeToScene(new Vector3(spacing_coeff*ortho.y, 0, spacing_coeff*ortho.x));

				createBuildings(x + spacing_coeff*ortho.x, y + spacing_coeff*ortho.y, road_angle, -toRoad);
				createBuildings(x - spacing_coeff*ortho.x, y - spacing_coeff*ortho.y, road_angle, toRoad);
			}	
		}
	}


	/* Instatiate agents and give them a home */
	private void createPopulation() {

		/* Pick random habitation */
		List<GameObject> currentBuildingsList = m_houses;
		List<int> available_idxs = Enumerable.Range(0, currentBuildingsList.Count).ToList();
		
		int habitation_idx = available_idxs[UnityEngine.Random.Range(0, available_idxs.Count)];

		/* Current house to be filled with inhabitants */
		GameObject go_habitation = currentBuildingsList[habitation_idx];

		/* Count the number of inhabitants added in the current house */
		int count = 0;

		for (int i = 0 ; i < POPULATION_SIZE ; i++) {

			/* Case current house is full, go to the next one */
			int capacity = go_habitation.GetComponent<Building>().GetCapacity();

			if (count >= capacity) {
				available_idxs.Remove(habitation_idx);
				habitation_idx = available_idxs[UnityEngine.Random.Range(0, available_idxs.Count)];

				/* Case no buildings left in the currents list, switch to the next authorized (i.e. buildings) */
				if (available_idxs.Count == 0) {
					if (currentBuildingsList == m_houses) {
						currentBuildingsList = m_buildings;
						available_idxs = Enumerable.Range(0, currentBuildingsList.Count).ToList();
						habitation_idx = available_idxs[UnityEngine.Random.Range(0, available_idxs.Count)];
					} else {
						Debug.Log("Exceeded cities capacity");
					}
					
				} 

				go_habitation = currentBuildingsList[habitation_idx];
				count = 0;
			}

			/* Instatiate in front of home on road but hide as if they are in */
			Vector3 position = go_habitation.transform.position + go_habitation.GetComponent<Building>().GetRoadDirection();
			GameObject go_agent = Instantiate(agent, new Vector3(position.x, 0.05f, position.z), Quaternion.identity);
			go_agent.transform.parent = this.transform;
			go_agent.GetComponent<MeshRenderer>().enabled = false;
			go_agent.transform.localScale = AGENT_DIMENSIONS;

			/* Randomly pick start and end times in time ranges for the agent */
			float agent_start = UnityEngine.Random.Range(early_start, late_start);
			float agent_end = UnityEngine.Random.Range(early_end, late_end);

			/* Initialize inhabitant properties and state */
			go_agent.GetComponent<Inhabitant>().Init(go_habitation, agent_start, agent_end);

			/* Add it to the home in question */
			if (!go_habitation.GetComponent<Building>().IsHabitable()) go_habitation.GetComponent<Building>().SwitchToHabitation();
			go_habitation.GetComponent<Building>().AddInhabitant(go_agent);
			count++;

			/* Save agent for later use */
			m_agents.Add(go_agent);
		}

	}

	/* Wrapped to add homeless inhabitant to list */
	public void AddHomeless(GameObject homeless) { m_homeless.Add(homeless); }

	/* Wrapped to add workless inhabitant to list */
	public void AddWorkless(GameObject workless) { m_workless.Add(workless); }

	/* Relocates inhabitants living in a building that has been destroyed by collisions */
	public void RelocateHomeless() {

		for (int i = m_homeless.Count - 1; i >= 0 ; i--) {

			for (int j =  m_habitations.Count - 1 ; j >= 0 ; j--) {

				GameObject go_habitation = m_habitations[j];

				/* Find the first habitation that isn't full */
				if(!go_habitation.GetComponent<Building>().IsFull()) {

					/* affect it new home */
					GameObject go_homeless = m_homeless[i];
					go_homeless.GetComponent<Inhabitant>().SetHome(go_habitation);
					go_habitation.GetComponent<Building>().AddInhabitant(go_homeless);

					/* Change position */
					Vector3 position = go_habitation.transform.position + go_habitation.GetComponent<Building>().GetRoadDirection();
					go_homeless.transform.position = position;
					m_homeless.Remove(go_homeless);
					break;
				}
			}
		}
	}

	/* Relocates inhabitants working in a building that has been destroyed by collisions */
	public void RelocateWorkless() {

		for (int i = m_workless.Count - 1; i >= 0 ; i--) {

			for (int j =  m_workplaces.Count - 1 ; j >= 0 ; j--) {

				GameObject go_workplace = m_workplaces[j];

				/* Find the first workplace that isn't full */
				if(!go_workplace.GetComponent<Building>().IsFull()) {

					/* Affect it new worplace */
					GameObject go_workless = m_workless[i];
					go_workless.GetComponent<Inhabitant>().SetWorkPlace(go_workplace);
					go_workplace.GetComponent<Building>().AddWorker(go_workless);

					/* Change position */
					Vector3 position = go_workplace.transform.position + go_workplace.GetComponent<Building>().GetRoadDirection();
					go_workless.transform.position = position;
					m_workless.Remove(go_workless);
					break;
				}
			}
		}
		
	}

	/* Splits buildings between habitations and workplaces */
	public void splitHabitationWorkplace() {

		for (int i = 0 ; i < m_houses.Count ; i++) {
				m_habitations.Add(m_houses[i]);
		}

		for (int i = 0 ; i < m_buildings.Count ; i++) {
			if (m_buildings[i].GetComponent<Building>().IsHabitable()) {
				m_habitations.Add(m_buildings[i]);
			} else {
				m_workplaces.Add(m_buildings[i]);
			}
		}

		for (int i = 0 ; i < m_skycrapers.Count ; i++) {
			if (m_skycrapers[i].GetComponent<Building>().IsHabitable()) {
				m_habitations.Add(m_skycrapers[i]);
			} else {
				m_workplaces.Add(m_skycrapers[i]);
			}
		}
	}


	/* Affect workplace to each agent */
	void affectWorkplaces() {

		/* Pick random workplace */
		List<int> available_idxs = Enumerable.Range(0, m_workplaces.Count).ToList();
		int workplace_idx = available_idxs[UnityEngine.Random.Range(0, available_idxs.Count)];
		GameObject go_workplace = m_workplaces[workplace_idx];
		int count = 0;

		for (int i = 0 ; i < m_agents.Count ; i++) {

			/* Switch to next building if current cant have more workers */
			if (go_workplace.GetComponent<Building>().IsFull()) {
				available_idxs.Remove(workplace_idx);
				workplace_idx = available_idxs[UnityEngine.Random.Range(0, available_idxs.Count)];
				go_workplace = m_workplaces[workplace_idx];
				count = 0;
			}

			/* Affect workplace to current agent */
			GameObject go_agent = m_agents[i];
			go_agent.GetComponent<Inhabitant>().SetWorkPlace(go_workplace);
			go_workplace.GetComponent<Building>().AddWorker(go_agent);
			count++;

		}

	}

	/* Give information on building status to other scripts */
	public bool getBuildStatus() { return done; }

	void Start ()
	{
        map=createMap();
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

		/* Add the points on the cercle */
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

        /* Add N random points */
		for (int i = 0; i < NPOINTS; i++) {
			colors.Add ((uint)0);
			/* Higher density points have ore chance to be selected */
			float[] rnd_coord = weighten_map[(int) UnityEngine.Random.Range(0, weighten_map.Count)];
			Vector2 vec = new Vector2(rnd_coord[0], rnd_coord[1]); 
			m_points.Add (vec);
		}

		/* Generate Graphs */
		Delaunay.Voronoi v = new Delaunay.Voronoi (m_points, colors, new Rect (0, 0, SIZE, SIZE));
		m_edges = v.VoronoiDiagram ();
		m_spanningTree = v.SpanningTree (KruskalType.MINIMUM);
		m_delaunayTriangulation = v.DelaunayTriangulation ();

		/* Instatiates buildings and roads */
		createStructures();

		/* build navmesh */
		surface.BuildNavMesh();

		/* Instatiate agents and affect them homes */
		createPopulation();

		/* Build lists of workplaces and habitations */
		splitHabitationWorkplace();

		/* Give a workplace to each agent */
		affectWorkplaces();	

	}

	void Update() {

		/* Ensure no agent is homeless */
		if (m_homeless.Count > 0) RelocateHomeless();

		/* Ensure no agent is workless */
		if (m_workless.Count > 0) RelocateWorkless();
		
		/* Check if build is over */
		if (m_homeless.Count == 0 && m_workless.Count == 0) done = true;

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