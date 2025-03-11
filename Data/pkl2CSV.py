# import pickle
# import pandas as pd
# import numpy as np
# import sys
# import warnings
# warnings.filterwarnings("ignore", category=DeprecationWarning)

# def adaptive_downsample(waveform, similarity_threshold=6, min_segment_length=3):
#     # returns tuple of (downsampled_values, segment_lengths)
#     downsampled = []
#     segment_lengths = []
    
#     i = 0
#     while i < len(waveform):
#         current_value = waveform[i]
#         segment_length = 1
        
#         j = i + 1
#         while j < len(waveform):
#             if abs(waveform[j] - current_value) <= similarity_threshold:
#                 segment_length += 1
#                 j += 1
#             else:
#                 break
        
#         if segment_length >= min_segment_length:
#             segment_mean = np.mean(waveform[i:i+segment_length])
#             downsampled.append(segment_mean)
#             segment_lengths.append(segment_length)
#             i += segment_length
#         else:
#             downsampled.append(current_value)
#             segment_lengths.append(1)
#             i += 1
            
#     return np.array(downsampled), np.array(segment_lengths)

# with open('./pkls/Mapia_Inauini.pkl', 'rb') as f:
#     data = pickle.load(f)

# def area_filter(lng, lat):
#     return (lng > -70 and lng < -68) and (lat > -9.75 and lat < -7.75)
#     # return (lng>-56.9) & (lng<-56.6) & (lat>-18.2) & (lat<-17.9)
#     # return (lng>-72) & (lng<-71) & (lat>46) & (lat<47) # default forest
#     # return (lng>-71.5) & (lng<-71.4) & (lat>46.5) & (lat<46.6) # small forest
#     # return (lng>-82.7) & (lng<-82.6) & (lat>51.7) & (lat<51.8)
#     # return (lng>-60.45) & (lng<-60.25) & (lat>2.25) & (lat<2.45)
#     # return True

# data_list = []

# print(data['rh'][0])
# print(data['prop'][0])


# sys.exit()

# for i in range(len(data['prop'])):
#     entry = data['prop'][i]
#     prop_rh = data['prop_rh'][i]
    
#     latitude = entry['geolocation/latitude_bin0']
#     longitude = entry['geolocation/longitude_bin0']
#     elevation = entry['geolocation/elevation_bin0']

#     if not area_filter(longitude, latitude):
#         continue

#     # Get instrument and lowestmode coordinates
#     instrument_lat = prop_rh['geolocation/latitude_instrument']
#     instrument_lon = prop_rh['geolocation/longitude_instrument']
#     instrument_alt = prop_rh['geolocation/altitude_instrument']
    
#     lowest_lat = prop_rh['lat_lowestmode']
#     lowest_lon = prop_rh['lon_lowestmode']
#     lowest_elev = prop_rh['elev_lowestmode']

#     # Get WGS84 elevation from digital elevation model
#     wgs84_elevation = prop_rh['geolocation/digital_elevation_model']

#     rh_2 = data['rh'][i][2]
#     rh_50 = data['rh'][i][50]
#     rh_98 = data['rh'][i][98]
#     rh_waveform = data['rh'][i]
#     rh_waveform = rh_waveform/rh_waveform.sum()*30 if rh_waveform.sum() > 0 else rh_waveform
#     rh_waveform_str = ','.join(map(str, rh_waveform))

#     # Adaptive downsampling of raw waveform
#     raw_waveform = data['y'][i]
#     downsampled_values, segment_lengths = adaptive_downsample(raw_waveform)
    
#     # Convert to strings and combine with delimiter
#     values_str = ','.join(map(str, downsampled_values))
#     lengths_str = ','.join(map(str, segment_lengths))

#     data_list.append({
#         'latitude': latitude,
#         'longitude': longitude,
#         'elevation': elevation,
#         'instrument_lat': instrument_lat,
#         'instrument_lon': instrument_lon, 
#         'instrument_alt': instrument_alt,
#         'lowest_lat': lowest_lat,
#         'lowest_lon': lowest_lon,
#         'lowest_elev': lowest_elev,
#         'wgs84_elevation': wgs84_elevation,
#         'rh2': rh_2,
#         'rh50': rh_50,
#         'rh98': rh_98,
#         'rh_waveform': rh_waveform_str,
#         'raw_waveform_values': values_str,
#         'raw_waveform_lengths': lengths_str,
#     })
#     print('iter: ', i)

# df = pd.DataFrame(data_list)
# df.to_csv('csvs/Mapia_Inauini.csv', index=False)


import pickle
import pandas as pd
import numpy as np
import sys
import warnings
import random

warnings.filterwarnings("ignore", category=DeprecationWarning)

# def adaptive_downsample(waveform, similarity_threshold=20, min_segment_length=3, target_sum = 30):
#     # returns tuple of (downsampled_values, segment_lengths)
#     downsampled = []
#     segment_lengths = []
    
#     i = 0
#     while i < len(waveform):
#         current_value = waveform[i]
#         segment_length = 1
        
#         j = i + 1
#         while j < len(waveform):
#             if abs(waveform[j] - current_value) <= similarity_threshold:
#                 segment_length += 1
#                 j += 1
#             else:
#                 break
        
#         if segment_length >= min_segment_length:
#             segment_mean = np.mean(waveform[i:i+segment_length])
#             downsampled.append(segment_mean)
#             segment_lengths.append(segment_length)
#             i += segment_length
#         else:
#             downsampled.append(current_value)
#             segment_lengths.append(1)
#             i += 1
    
#     return np.array(downsampled), np.array(segment_lengths)

def adaptive_downsample(waveform, similarity_threshold=0, min_segment_length=3):
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


with open('./pkls/Forest_cs237_2024_ISS.pkl', 'rb') as f:
    data = pickle.load(f)

def area_filter(lng, lat):
    # random_number = random.randint(0, 24)

    # bounds = (lng >= -70 and lng <= -68) and (lat >= -9.75 and lat <= -7.75)

    # if random_number == 0 and bounds:
    #     return True
    # return (lng>-56.9) & (lng<-56.6) & (lat>-18.2) & (lat<-17.9)
    # return (lng>-72) & (lng<-71) & (lat>46) & (lat<47) # default forest
    return (lng>-71.6) & (lng<-71.4) & (lat>46.4) & (lat<46.6) # small forests
    # return (lng>-82.7) & (lng<-82.6) & (lat>51.7) & (lat<51.8)
    # return (lng>-60.45) & (lng<-60.25) & (lat>2.25) & (lat<2.45)
    # return True

data_list = []


# print(data.keys())
# print(data['prop'][0])
# print(data['rh'][0])

# sys.exit()

for i in range(len(data['prop'])):
    entry = data['prop'][i]
    prop_rh = data['prop_rh'][i]
    
    latitude = entry['geolocation/latitude_bin0']
    longitude = entry['geolocation/longitude_bin0']
    elevation = entry['geolocation/elevation_bin0']

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
    
    # Convert to strings and combine with delimiter
    values_str = ','.join(map(str, downsampled_values))
    lengths_str = ','.join(map(str, segment_lengths))
    positions_str = ','.join(map(str, physical_positions))

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
    print('iter: ', i)

df = pd.DataFrame(data_list)
df.to_csv('csvs/forest_adapt0_medium.csv', index=False)
