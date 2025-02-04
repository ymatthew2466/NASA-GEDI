import pickle
import pandas as pd
import warnings
warnings.filterwarnings("ignore", category=DeprecationWarning)

with open('./GEDI_sample_for_CS237_2024.pkl', 'rb') as f:
    data = pickle.load(f)

downsample_factor = 20
waveform_downsample_factor = 1


def area_filter(lng, lat):
    # return (lng>-60.45) & (lng<-60.25) & (lat>2.25) & (lat<2.45)
    # return (lng>-56.9) & (lng<-56.6) & (lat>-18.2) & (lat<-17.9)
    return (lng>-82.7) & (lng<-82.6) & (lat>51.7) & (lat<51.8)

data_list = []
for i in range(len(data['prop'])):

    entry = data['prop'][i]

    # extracting values
    latitude = entry['geolocation/latitude_bin0']
    longitude = entry['geolocation/longitude_bin0']
    elevation = entry['geolocation/elevation_bin0']

    if not area_filter(longitude, latitude):
        continue

    # rh metrics
    rh_2 = data['rh'][i][2]
    rh_50 = data['rh'][i][50]
    rh_98 = data['rh'][i][98]
    rh_waveform = data['rh'][i]
    rh_waveform = rh_waveform/rh_waveform.sum()*30
    rh_waveform_str = ','.join(map(str, rh_waveform))

    # raw waveform
    raw_waveform = data['y'][i]
    raw_waveform_downsampled = raw_waveform[::waveform_downsample_factor]
    raw_waveform_str = ','.join(map(str, raw_waveform_downsampled))

    # append data
    data_list.append({
        'latitude': latitude,
        'longitude': longitude,
        'elevation': elevation,
        'rh2': rh_2,
        'rh50': rh_50,
        'rh98': rh_98,
        'rh_waveform': rh_waveform_str,
        'raw_waveform': raw_waveform_str,
    })
    print('iter: ', i)


df = pd.DataFrame(data_list)
df.to_csv('forest.csv', index=False)

# if isinstance(data, list):
#     print(f"The file contains a list with {len(data)} elements.")
#     if len(data) > 0:
#         print((data[0][0]))
#         print(len(data[0][100]))


# data_list = []
# for i in range(len(data[1])):
#     # continue

#     if i % downsample_factor != 0:
#         continue

#     # entry = data['prop'][i]
#     entry = data[1][i]  # dict


#     # extracting values
#     latitude = entry['geolocation/latitude_bin0']
#     longitude = entry['geolocation/longitude_bin0']
#     elevation = entry['geolocation/elevation_bin0']

#     # rh metrics
#     rh_2 = f"{float(entry['elev_highestreturn']) - float(entry['elev_lowestmode'])}"
#     rh_50 = f"{float(entry['elev_highestreturn']) - float(entry['elev_lowestmode'])}"
#     rh_98 = f"{float(entry['elev_highestreturn']) - float(entry['elev_lowestmode'])}"

#     rh_waveform = data[0][i]
#     rh_waveform_str = ','.join(map(str, rh_waveform))

#     # raw waveform
#     raw_waveform = data[0][i]
#     raw_waveform_downsampled = raw_waveform[::waveform_downsample_factor]
#     raw_waveform_str = ','.join(map(str, raw_waveform_downsampled))

#     # append data
#     data_list.append({
#         'latitude': latitude,
#         'longitude': longitude,
#         'elevation': elevation,
#         'rh2': rh_2,
#         'rh50': rh_50,
#         'rh98': rh_98,
#         'rh_waveform': rh_waveform_str,
#         'raw_waveform': raw_waveform_str,
#     })
#     print('iter: ', i)


# df = pd.DataFrame(data_list)
# df.to_csv('rainforest.csv', index=False)


