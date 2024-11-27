import cv2
import csv
import sys
import mediapipe as mp
import numpy as np
from mediapipe.python.solutions.pose import PoseLandmark

def initialize_pose():
    return mp.solutions.pose.Pose(
        min_detection_confidence=0.8,
        min_tracking_confidence=0.8,
        model_complexity=2,
        enable_segmentation=True,
        smooth_segmentation=True,
        smooth_landmarks=True
    )

def initialize_video_writer(video_capture, output_path):
    frame_height = int(video_capture.get(cv2.CAP_PROP_FRAME_HEIGHT))
    frame_width = int(video_capture.get(cv2.CAP_PROP_FRAME_WIDTH))
    fps = video_capture.get(cv2.CAP_PROP_FPS)
    fourcc = cv2.VideoWriter_fourcc(*'mp4v')
    return cv2.VideoWriter(output_path, fourcc, fps, (frame_width, frame_height))

def write_csv_header(csv_writer, csvType):
    header = ['frame_number', 'timestamp']
    
    # coord
    if csvType == 'coord':
        for landmark in PoseLandmark:
            header.extend([f'{landmark.name}_x', f'{landmark.name}_y', f'{landmark.name}_visibility'])
        csv_writer.writerow(header)
    
    # gradient
    elif csvType == 'g':
        for i in range(26):
            header.append(f"G{i}")
        csv_writer.writerow(header)

    # absolute angles
    elif csvType == 'a':
        for i in range(26):
            header.append(f"A{i}")
        csv_writer.writerow(header)

    # relative angles
    elif csvType == 'r':
        for i in range(8):
            header.append(f"R{i}")
        csv_writer.writerow(header)

def process_frame(frame, pose, frame_count, timestamp):
    frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    frame_rgb.flags.writeable = False
    results = pose.process(frame_rgb)
    frame_rgb.flags.writeable = True
    frame_bgr = cv2.cvtColor(frame_rgb, cv2.COLOR_RGB2BGR)
    
    return frame_bgr, results

def draw_pose_landmarks(frame, pose_landmarks):
    mp.solutions.drawing_utils.draw_landmarks(
        frame, 
        pose_landmarks, 
        mp.solutions.pose.POSE_CONNECTIONS,
        landmark_drawing_spec=mp.solutions.drawing_styles.get_default_pose_landmarks_style()
    )

def add_text_into_vid_frame(frame, frame_count, timestamp):
    cv2.putText(
        frame,  
        f'frame {frame_count}  {timestamp}',  
        (50, 50),  
        cv2.FONT_HERSHEY_SIMPLEX, 1,  
        (0, 255, 255),  
        2,  
        cv2.LINE_4
    ) 

def extract_landmark_data(pose_landmarks, previous_landmarks):
    if pose_landmarks:
        current_landmarks = []
        for i, landmark in enumerate(pose_landmarks.landmark):
            if landmark.x == 0 and landmark.y == 0 and previous_landmarks is not None:
                current_landmarks.append(previous_landmarks[i])
            else:
                current_landmarks.append(landmark)
        return current_landmarks
    return None

def calculate_metrics(landmarks):
    def calculate_relative_angle(a, b, c):        
        ba = np.array([a.x - b.x, a.y - b.y])
        bc = np.array([c.x - b.x, c.y - b.y])
        
        cosine_angle = np.dot(ba, bc) / (np.linalg.norm(ba) * np.linalg.norm(bc))
        angle = np.arccos(np.clip(cosine_angle, -1.0, 1.0))
        return angle
    
    landmark_triplets = [
        (11, 13, 15), (12, 14, 16),
        (13, 11, 23), (11, 23, 25), (23, 25, 27),
        (14, 12, 24), (12, 24, 26), (24, 26, 28)
    ]

    relative_angles = []

    for a, b, c in landmark_triplets:
        angle = calculate_relative_angle(landmarks[a], landmarks[b], landmarks[c])
        relative_angles.append(angle)

    return relative_angles

def main():
    # change the input path here ("file path" + "video name")
    input_vid_name = "wholeGarden_webcamCUT"
    input_vid_path = "Python Scripts/origin_vids/" + input_vid_name + ".mp4"
    
    # change the output file paths here
    # c = model complexity
    output_video_path = f"Python Scripts/results/videos/{input_vid_name}_"     + "annotated.mp4"
    # r = relative angle
    output_r_csv_path = f"Python Scripts/results/poses/{input_vid_name}_"      + "legacy_edit.csv"

    pose = initialize_pose()
    vid = cv2.VideoCapture(input_vid_path)
    vid_writer = initialize_video_writer(vid, output_video_path)

    with open(output_r_csv_path, 'w', newline='') as r_csvfile:
        
        r_csv_writer = csv.writer(r_csvfile)
        
        # write header to csv file
        write_csv_header(r_csv_writer,'r')

        frame_count = 0
        previous_landmarks = None

        while True:
            ret, frame = vid.read()
            if not ret:
                break
            
            # annotate video
            frame, results = process_frame(frame, pose, frame_count, vid.get(cv2.CAP_PROP_POS_MSEC))
            
            if results.pose_landmarks:
                draw_pose_landmarks(frame, results.pose_landmarks)
            
            # add text to the frame, then write it on output video
            add_text_into_vid_frame(frame, frame_count, vid.get(cv2.CAP_PROP_POS_MSEC))
            vid_writer.write(frame)

            row_starter = [frame_count, vid.get(cv2.CAP_PROP_POS_MSEC)]
            r_row = row_starter.copy()

            current_landmarks = extract_landmark_data(results.pose_landmarks, previous_landmarks)
            
            if current_landmarks:
                
                # other metrics
                for landmark in current_landmarks:
                    arrOfRelAngles = calculate_metrics(current_landmarks)
                r_row.extend(arrOfRelAngles)

                previous_landmarks = current_landmarks
            else:
                r_row.extend([0.0] * 8)

            r_csv_writer.writerow(r_row)


            frame_count += 1

    pose.close()
    vid.release()
    vid_writer.release()

if __name__ == "__main__":
    main()