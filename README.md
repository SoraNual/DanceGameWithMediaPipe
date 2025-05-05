# เกมตรวจจับท่าเต้นจากวิดีโอและกล้องเว็บแคม ด้วยเครื่องมือ MediaPipe พร้อมระบบให้คะแนนแบบกึ่งเรียลไทม์
โครงงานนี้เป็นส่วนหนึ่งของวิชา 01418499 โครงงานวิทยาการคอมพิวเตอร์ ประจำภาคต้น 2567 ภาควิชาวิทยาการคอมพิวเตอร์ มหาวิทยาลัยเกษตรศาสตร์บางเขน โดยมีอ.ธนบูรณ์ ทองบัวศิริไล เป็นอาจารย์ที่่ปรึกษา สงวนสิทธิ์ในการใช้งาน ตามข้อบังคับของมหาวิทยาลัยเกษตรศาสตร์

เกมเต้นของโครงงานนี้ถูกสร้างขึ้น เนื่องจากปัจจุบันเกมเต้นที่สามารถเล่นผ่านคอมพิวเตอร์หรือแล็ปท็อปในปัจจุบันนั้นจำเป็นต้องใช้ Motion Controller ในการเล่น ทำให้อาจไม่สะดวกต่อผู้เล่นเนื่องจากต้องมีการจับโทรศัพท์ในการเล่นอยู่ตลอดเวลา คณะผู้จัดทำจึงต้องการพัฒนาเกมเต้นที่สามารถเล่นได้โดยการใช้เพียงกล้องเว็บแคมที่เชื่อมต่อกับคอมพิวเตอร์ของผู้เล่น โดยอาศัย MediaPipe Pose เพื่อคาดคะเนพิกัดของข้อต่อแล้วนำข้อมูลมาใช้คำนวณและคิดคะแนนความเหมือนกับท่าทางที่ผู้เล่นต้องทำตาม

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at:
https://www.apache.org/licenses/LICENSE-2.0

## Additional Restrictions
- Any derivative works, redistributions, or modifications must provide clear attribution to the Computer Science Department, Faculty of Science, Kasetsart University.
- Commercial use of this software or any derivative works requires explicit written permission from the Computer Science Department, Faculty of Science, Kasetsart University.
- Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

## Project Structure
- `Python Scripts/`
  - `pose_extAnno_csv.py`: สกัดพิกัดจากคลิปแล้วบันทึกลงไฟล์ csv และ annotate landmark ออกมาเป็นอีกคลิปแยกไว้ในรูปแบบ {ชื่อเพลง}_annotated.mp4
  - `pose_extAnno_JSON.py`: สกัดพิกัดจากคลิปแล้วบันทึกลงไฟล์ JSON และ annotate landmark ออกมาเป็นอีกคลิปแยกไว้ในรูปแบบ {ชื่อเพลง}_legacy.mp4
  - `pose_compareVIds.py`: สกัดพิกัดจากอีกคลิปแล้วเปรียบเทียบกับข้อมูลจากไฟล์ csv ที่ระบุไว้
  - `origin_vids/`: โฟลเดอร์สำหรับใส่คลิปต้นแบบ
  - `results/videos/`: โฟลเดอร์สำหรับคลิปที่ถูก annotate ด้วย landmark ซึ่งมักถูกใช้เพื่อการตรวจสอบความถูกต้องของพิกัดต่าง ๆ ที่ประมวลผลออกไปได้
  - `results/poses/`: โฟลเดอร์สำหรับเก็บข้อมูลของมุมที่คำนวณออกมาจากการสกัดพิกัดคลิปต่าง ๆ โดยมีทั้งรูปแบบ .csv และ .JSON

## ผู้จัดทำ

- นายสรวิชญ์ นวลมณี
- นายรชต แก้ววิเศษ
