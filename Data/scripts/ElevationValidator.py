import pickle
import matplotlib.pyplot as plt
import matplotlib.image as mpimg
import numpy as np
import rasterio
import warnings
from rasterio.errors import NotGeoreferencedWarning
warnings.filterwarnings('ignore', category=NotGeoreferencedWarning)
warnings.filterwarnings('ignore', category=DeprecationWarning)


# complete bounds for texture
GEO_BOUNDS = [-68.75, -66.5, -20.95, -19.05]
IMAGE_PATH = '/Users/matthewyoon/Documents/cs2370/GEDI/Data/terrainFiles/Bolivia-Saltflats_TandemX_30m.tif'
PKL_FILE = '../pkls/Bolivia_saltflats.pkl'


def area_filter(lng, lat):
    bounds = (lng > GEO_BOUNDS[0]) & (lng < GEO_BOUNDS[1]) & (lat > GEO_BOUNDS[2]) & (lat < GEO_BOUNDS[3])
    return bounds


# returns list of relative coords
def coordinate_picker():

    with rasterio.open(IMAGE_PATH) as src:
        dem_data = src.read(1)  # read first band (elevation data)
    
    # create figure and display im
    fig, ax = plt.subplots()
    img_plot = ax.imshow(dem_data, cmap='terrain')
    plt.colorbar(img_plot, ax=ax, label='Elevation')
    ax.set_title("Click points ('enter' to continue)")
    
    coordinates = []
    
    # mouse click handler
    def onclick(event):
        if event.xdata is not None and event.ydata is not None:
            # absolute coords
            abs_x = int(round(event.xdata))
            abs_y = int(round(event.ydata))

            # relative coords
            rel_x = event.xdata / dem_data.shape[1]
            rel_y = event.ydata / dem_data.shape[0]

            # geographic coords
            range_long = GEO_BOUNDS[1] - GEO_BOUNDS[0]
            range_lat = GEO_BOUNDS[3] - GEO_BOUNDS[2]
            long = rel_x * range_long + GEO_BOUNDS[0]
            lat = rel_y * range_lat + GEO_BOUNDS[2]


            if (abs_x < 0 or abs_x >= dem_data.shape[1] or abs_y < 0 or abs_y >= dem_data.shape[0]):
                return

            # mark clicked
            ax.plot(event.xdata, event.ydata, 'ro', markersize=5)
            fig.canvas.draw()

            # get elevation value
            elevation = dem_data[abs_y, abs_x]
            
            coordinates.append((long, lat, elevation))            
                                    
    
    # connect event handler
    cid = fig.canvas.mpl_connect('button_press_event', onclick)
    
    # key press for exit
    def onkey(event):
        if event.key == 'enter':
            plt.close(fig)
    
    fig.canvas.mpl_connect('key_press_event', onkey)
    
    plt.show()
    
    return coordinates, dem_data


# prints elevation difference of closest GEDI point
def matchGEDI(coords, dem_data):
    with open(PKL_FILE, 'rb') as f:
        data = pickle.load(f)
    
    # each clicked point
    for user_coord in coords:
        user_long, user_lat, user_elev = user_coord
        print(f"Processing TIF point: ({user_long:.6f}, {user_lat:.6f}), elevation: {user_elev:.2f}m")
        
        closest_point = None
        min_dist = float('inf')
        
        # all GEDI
        for i in range(len(data['prop'])):
            entry = data['prop'][i]
            prop_rh = data['prop_rh'][i]
            
            latitude = prop_rh['lat_lowestmode']
            longitude = prop_rh['lon_lowestmode']
            elevation = entry['elev_lowestmode']
            
            if not area_filter(longitude, latitude):
                continue
            
            # euclidean dist
            dist = np.sqrt((longitude - user_long)**2 + (latitude - user_lat)**2)
            
            # update closest
            if dist < min_dist:
                min_dist = dist
                closest_point = {
                    'longitude': longitude,
                    'latitude': latitude,
                    'elevation': elevation,
                    'distance': dist,
                    'index': i
                }
        
        
        if closest_point:
            # convert geo coords back to pixel coords
            range_long = GEO_BOUNDS[1] - GEO_BOUNDS[0]
            range_lat = GEO_BOUNDS[3] - GEO_BOUNDS[2]

            # convert GEDI to relative coordinates
            rel_x = (closest_point['longitude'] - GEO_BOUNDS[0]) / range_long
            rel_y = (closest_point['latitude'] - GEO_BOUNDS[2]) / range_lat

            # Convert to pixel coordinates
            pixel_x = int(round(rel_x * dem_data.shape[1]))
            pixel_y = int(round(rel_y * dem_data.shape[0]))
            pixel_x = max(0, min(pixel_x, dem_data.shape[1] - 1))
            pixel_y = max(0, min(pixel_y, dem_data.shape[0] - 1))

            tif_elevation_at_gedi = dem_data[pixel_y, pixel_x]


            print(f"\tGEDI Point: \t\t\t({closest_point['longitude']:.6f}, {closest_point['latitude']:.6f})")
            print(f"\tGEDI Elevation: \t\t{closest_point['elevation']:.2f}m")
            print(f"\tTIF Elevation at GEDI Point: \t{tif_elevation_at_gedi:.2f}m")
            print(f"\tElevation difference: \t\t{abs(tif_elevation_at_gedi - closest_point['elevation']):.2f}m")
            print()
    

    
if __name__ == "__main__":
    coords, dem_data = coordinate_picker()
    matchGEDI(coords, dem_data)