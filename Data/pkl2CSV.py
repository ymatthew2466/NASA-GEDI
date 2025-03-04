# import pickle
# import pandas as pd
# import numpy as np
# import sys
# import warnings
# warnings.filterwarnings("ignore", category=DeprecationWarning)

# def adaptive_downsample(waveform, similarity_threshold=1, min_segment_length=3):
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

# with open('./pkls/Pantanal_cs237_2024_ISS.pkl', 'rb') as f:
#     data = pickle.load(f)

# # print(data['prop_rh'][0])
# # print(data['prop'][0])
# # print(data['rh'][0])
# # print(data['y'][0])
# # sys.exit()



# def area_filter(lng, lat):
#      # return (lng>-60.45) & (lng<-60.25) & (lat>2.25) & (lat<2.45)
#     return (lng>-56.9) & (lng<-56.6) & (lat>-18.2) & (lat<-17.9)
# #     return (lng>-82.7) & (lng<-82.6) & (lat>51.7) & (lat<51.8)
#     # return (lng>-82.7) & (lng<-82.6) & (lat>51.7) & (lat<51.8)

# data_list = []
# for i in range(len(data['prop'])):
#     entry = data['prop'][i]
    
#     latitude = entry['geolocation/latitude_bin0']
#     longitude = entry['geolocation/longitude_bin0']
#     elevation = entry['geolocation/elevation_bin0']

#     if not area_filter(longitude, latitude):
#         continue

#     # temp naive downsampling
#     # if i % 50 != 0:
#     #     continue

#     rh_2 = data['rh'][i][2]
#     rh_50 = data['rh'][i][50]
#     rh_98 = data['rh'][i][98]
#     rh_waveform = data['rh'][i]
#     rh_waveform = rh_waveform/rh_waveform.sum()*30
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
#         'rh2': rh_2,
#         'rh50': rh_50,
#         'rh98': rh_98,
#         'rh_waveform': rh_waveform_str,
#         'raw_waveform_values': values_str,
#         'raw_waveform_lengths': lengths_str,
#     })
#     print('iter: ', i)

# df = pd.DataFrame(data_list)
# df.to_csv('rainforest_adaptive_sim5.csv', index=False)



# # WITH RH CUTOFF
# import pickle
# import pandas as pd
# import numpy as np
# import sys
# import warnings
# warnings.filterwarnings("ignore", category=DeprecationWarning)

# def adaptive_downsample(waveform, similarity_threshold=0.5, min_segment_length=3):
#     """
#     Returns: tuple of (downsampled_values, segment_lengths)
#     """
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

# with open('./pkls/Pantanal_cs237_2024_ISS.pkl', 'rb') as f:
#     data = pickle.load(f)

# def area_filter(lng, lat):
#     return (lng>-56.9) & (lng<-56.6) & (lat>-18.2) & (lat<-17.9)

# data_list = []
# for i in range(len(data['prop'])):
#     entry = data['prop'][i]
    
#     latitude = entry['geolocation/latitude_bin0']
#     longitude = entry['geolocation/longitude_bin0']
#     elevation = entry['geolocation/elevation_bin0']

#     if not area_filter(longitude, latitude):
#         continue

#     rh_2 = data['rh'][i][2]
#     rh_50 = data['rh'][i][50]
#     rh_98 = data['rh'][i][98]
    
#     # Get heights and waveform
#     heights = data['rh'][i]
#     waveform = data['y'][i]
    
#     # Calculate proportion of waveform to keep based on RH98
#     # Find max height index and value right at or after RH98
#     # rh98_value = entry['rh_98']
#     rh_cutoff = data['rh'][i][90]
#     rh98_idx = None
#     for idx, h in enumerate(heights):
#         if h >= rh_cutoff:
#             rh98_idx = idx
#             break
    
#     if rh98_idx is None:
#         rh98_idx = len(heights) - 1  # Use the last index if RH98 exceeds all heights
    
#     # Calculate the proportion of the height range that RH98 represents
#     height_range_proportion = rh98_idx / (len(heights) - 1)
    
#     # Apply this proportion to the waveform length to find where to cut
#     waveform_cutoff_index = int(height_range_proportion * (len(waveform) - 1))
    
#     # Ensure we have at least one value
#     waveform_cutoff_index = max(1, waveform_cutoff_index)
    
#     # Trim the waveform
#     trimmed_waveform = waveform[:waveform_cutoff_index + 1]
    
#     # Process the RH waveform 
#     rh_waveform = heights.copy()
#     rh_waveform = rh_waveform/rh_waveform.sum()*30 if rh_waveform.sum() > 0 else rh_waveform
#     rh_waveform_str = ','.join(map(str, rh_waveform))

#     # Adaptive downsampling of trimmed waveform
#     downsampled_values, segment_lengths = adaptive_downsample(trimmed_waveform)
    
#     # Convert to strings and combine with delimiter
#     values_str = ','.join(map(str, downsampled_values))
#     lengths_str = ','.join(map(str, segment_lengths))

#     data_list.append({
#         'latitude': latitude,
#         'longitude': longitude,
#         'elevation': elevation,
#         'rh2': rh_2,
#         'rh50': rh_50,
#         'rh98': rh_98,
#         'rh_waveform': rh_waveform_str,
#         'raw_waveform_values': values_str,
#         'raw_waveform_lengths': lengths_str,
#     })
#     # print('iter: ', i)

# df = pd.DataFrame(data_list)
# df.to_csv('rainforest_adaptive_sim05_trimmed.csv', index=False)



# WITH INSTRUMENT COORDS
import pickle
import pandas as pd
import numpy as np
import sys
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning)

def adaptive_downsample(waveform, similarity_threshold=6, min_segment_length=3):
    # returns tuple of (downsampled_values, segment_lengths)
    downsampled = []
    segment_lengths = []
    
    i = 0
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
        
        if segment_length >= min_segment_length:
            segment_mean = np.mean(waveform[i:i+segment_length])
            downsampled.append(segment_mean)
            segment_lengths.append(segment_length)
            i += segment_length
        else:
            downsampled.append(current_value)
            segment_lengths.append(1)
            i += 1
            
    return np.array(downsampled), np.array(segment_lengths)

with open('./pkls/Forest_cs237_2024_ISS.pkl', 'rb') as f:
    data = pickle.load(f)

def area_filter(lng, lat):
    # return (lng>-56.9) & (lng<-56.6) & (lat>-18.2) & (lat<-17.9)
    # return (lng>-72) & (lng<-71) & (lat>46) & (lat<47) # default forest
    return (lng>-71.5) & (lng<-71.4) & (lat>46.5) & (lat<46.6) # small forest
    # return (lng>-82.7) & (lng<-82.6) & (lat>51.7) & (lat<51.8)
    # return (lng>-60.45) & (lng<-60.25) & (lat>2.25) & (lat<2.45)
    # return True

data_list = []
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
    rh_waveform = rh_waveform/rh_waveform.sum()*30 if rh_waveform.sum() > 0 else rh_waveform
    rh_waveform_str = ','.join(map(str, rh_waveform))

    # Adaptive downsampling of raw waveform
    raw_waveform = data['y'][i]
    downsampled_values, segment_lengths = adaptive_downsample(raw_waveform)
    
    # Convert to strings and combine with delimiter
    values_str = ','.join(map(str, downsampled_values))
    lengths_str = ','.join(map(str, segment_lengths))

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
    })
    print('iter: ', i)

df = pd.DataFrame(data_list)
df.to_csv('forest_adapt_6_small.csv', index=False)
