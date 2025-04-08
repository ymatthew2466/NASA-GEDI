import pickle
import pandas as pd
import numpy as np
import sys
import warnings
import random
warnings.filterwarnings("ignore", category=DeprecationWarning)

# prints PKL data and stops program
DEBUG_MODE = False

# path to PKL file
PKL_FILE = './pkls/Bolivia_saltflats.pkl'

# processing parameters
ADAPTIVE_THRESHOLD = 0
GEO_BOUNDS = [-67.8, -67.7, -20.5, -20.4]
CLIP_METERS_ABOVE_RH98 = 5

# CSV output
OUTPUT_PATH = '/Users/matthewyoon/Documents/cs2370/GEDI/Unity/GEDI_Visualization/Assets/Data/'
OUTPUT_FILENAME = 'bolivia_clip_5.csv'


def adaptive_downsample(waveform, similarity_threshold=ADAPTIVE_THRESHOLD, min_segment_length=3):
    # returns tuple of (downsampled_values, segment_lengths, physical_positions)
    downsampled = []
    segment_lengths = []
    physical_positions = []
    
    total_original_samples = len(waveform)
    
    i = 0
    accumulated_samples = 0
    while i < len(waveform):
        current_value = waveform[i]
        segment_length = 1
        
        j = i + 1
        while j < len(waveform):
            if abs(waveform[j] - current_value) <= similarity_threshold:
                segment_length += 1
                j += 1
            else:
                break
        
        # calc physical position as percentage of total
        physical_position = accumulated_samples / total_original_samples
        
        if segment_length >= min_segment_length:
            segment_mean = np.mean(waveform[i:i+segment_length])
            # segment_mean = waveform[i]
            downsampled.append(segment_mean)
            segment_lengths.append(segment_length)
            physical_positions.append(physical_position)
            accumulated_samples += segment_length
            i += segment_length
        else:
            downsampled.append(current_value)
            segment_lengths.append(1)
            physical_positions.append(physical_position)
            accumulated_samples += 1
            i += 1
    
    return np.array(downsampled), np.array(segment_lengths), np.array(physical_positions)


with open(PKL_FILE, 'rb') as f:
    data = pickle.load(f)

def area_filter(lng, lat):
    # random_number = random.randint(0, 10)

    # bounds = (lng >= -70 and lng <= -68) and (lat >= -9.75 and lat <= -7.75)

    # if random_number == 0 and bounds:
    #     return True
    # return (lng>-72) & (lng<-71) & (lat>46) & (lat<47) # default forest
    # return (lng>-71.6) & (lng<-71.4) & (lat>46.4) & (lat<46.6) # small forests
    # return (lng>-70) & (lng<-68) & (lat>-9.75) & (lat<-7.75)  # mapia
    # return (lng>-69) & (lng<-68.5) & (lat>-9) & (lat<-8.5)  # mapia small
    # bounds = (lng>-67.8) & (lng<-67.3) & (lat>-20.5) & (lat<-19.9)  # bolivia small
    # bounds = (lng>-67.8) & (lng<-67.7) & (lat>-20.5) & (lat<-20.4)  # bolivia smaller
    bounds = (lng > GEO_BOUNDS[0]) & (lng < GEO_BOUNDS[1]) & (lat > GEO_BOUNDS[2]) & (lat < GEO_BOUNDS[3])
    return bounds


data_list = []

# Print data keys in debug mode
if DEBUG_MODE:
    print(f'keys: {data.keys()}\n')
    print(f"prop: {data['prop'][0]}\n")
    print(f"prop_rh: {data['prop_rh'][0]}\n")
    print(f"rh: {data['rh'][0]}\n")
    sys.exit()

# Print stats
if ADAPTIVE_THRESHOLD == 0:
    print(f'Adaptive sampling threshold: \t{ADAPTIVE_THRESHOLD} (off)')
else:
    print(f'Adaptive sampling threshold: \t{ADAPTIVE_THRESHOLD}')
print(f'Meters clipped above RH98: \t{CLIP_METERS_ABOVE_RH98}m')

count = 0

for i in range(len(data['prop'])):
    # print waveform count
    count += 1
    print(f'\rWaveforms processed: \t\t{count}', end='')

    entry = data['prop'][i]
    prop_rh = data['prop_rh'][i]
    
    latitude = entry['geolocation/latitude_bin0']
    longitude = entry['geolocation/longitude_bin0']
    # elevation = entry['geolocation/elevation_bin0']
    elevation = entry['elev_lowestmode']
    elevation_bin0 = entry['geolocation/elevation_bin0']

    if not area_filter(longitude, latitude):
        continue

    # Get instrument and lowestmode coordinates
    instrument_lat = prop_rh['geolocation/latitude_instrument']
    instrument_lon = prop_rh['geolocation/longitude_instrument']
    instrument_alt = prop_rh['geolocation/altitude_instrument']
    
    lowest_lat = prop_rh['lat_lowestmode']
    lowest_lon = prop_rh['lon_lowestmode']
    lowest_elev = prop_rh['elev_lowestmode']

    # Get WGS84 elevation from digital elevation model
    wgs84_elevation = prop_rh['geolocation/digital_elevation_model']

    rh_2 = data['rh'][i][2]
    rh_50 = data['rh'][i][50]
    rh_98 = data['rh'][i][98]
    rh_waveform = data['rh'][i]
    # CHECK
    rh_waveform = rh_waveform/rh_waveform.sum()*30 if rh_waveform.sum() > 0 else rh_waveform
    rh_waveform_str = ','.join(map(str, rh_waveform))

    # Adaptive downsampling of raw waveform
    raw_waveform = data['y'][i]
    # normalize raw waveform here
    downsampled_values, segment_lengths, physical_positions = adaptive_downsample(raw_waveform)

    clip_height_above_ground = rh_98 + CLIP_METERS_ABOVE_RH98
    clip_elevation_threshold = elevation + clip_height_above_ground

    waveform_vertical_range = max(0.01, elevation_bin0 - elevation)

    clipped_values = []
    clipped_lengths = []
    clipped_positions = []

    if downsampled_values.size > 0:
        for j in range(len(downsampled_values)):
            sample_elevation = elevation_bin0 - (physical_positions[j] * waveform_vertical_range)

            if sample_elevation <= clip_elevation_threshold:
                clipped_values.append(downsampled_values[j])
                clipped_lengths.append(segment_lengths[j])
                clipped_positions.append(physical_positions[j])

    if not clipped_values:
        values_str = ""
        lengths_str = ""
        positions_str = ""
    else:
        values_str = ','.join(map(str, clipped_values))
        lengths_str = ','.join(map(str, clipped_lengths))
        positions_str = ','.join(map(str, clipped_positions))
    
    # # Convert to strings and combine with delimiter
    # values_str = ','.join(map(str, downsampled_values))
    # lengths_str = ','.join(map(str, segment_lengths))
    # positions_str = ','.join(map(str, physical_positions))

    data_list.append({
        'latitude': latitude,
        'longitude': longitude,
        'elevation': elevation,
        'instrument_lat': instrument_lat,
        'instrument_lon': instrument_lon, 
        'instrument_alt': instrument_alt,
        'lowest_lat': lowest_lat,
        'lowest_lon': lowest_lon,
        'lowest_elev': lowest_elev,
        'wgs84_elevation': wgs84_elevation,
        'rh2': rh_2,
        'rh50': rh_50,
        'rh98': rh_98,
        'rh_waveform': rh_waveform_str,
        'raw_waveform_values': values_str,
        'raw_waveform_lengths': lengths_str,
        'raw_waveform_positions': positions_str,
    })

df = pd.DataFrame(data_list)
df.to_csv(f'{OUTPUT_PATH}{OUTPUT_FILENAME}', index=False)
print(f'\nOutput path: \t\t\t{OUTPUT_PATH}')
print(f'Output filename: \t\t{OUTPUT_FILENAME}')