### [Tower of Hanoi](https://en.wikipedia.org/wiki/Tower_of_Hanoi) game solution: visualization and auralization

Published: https://www.youtube.com/watch?v=GikVwLac02A

There is a prime interval (2, 3, 5, 7, 11,..) corresponding to each disk.
When a disk is moved down we play an inversion about skipped intervals (also making it higher due octave equivalence):
```
Level 1
  State 1/2. Move disk 2; skipped none -> play 2
Level 2
  State 1/4. Move disk 3; skipped 2 -> play 8/3
  State 2/4. Move disk 2; skipped none -> play 2
  State 3/4. Move disk 3; skipped none -> play 3
Level 3
  State 1/8. Move disk 5; skipped 2,3 -> play 12/5
  State 2/8. Move disk 3; skipped 2 -> play 8/3
  State 3/8. Move disk 5; skipped 2 -> play 16/5
  State 4/8. Move disk 2; skipped none -> play 2
  State 5/8. Move disk 5; skipped 2,3 -> play 12/5
  State 6/8. Move disk 3; skipped none -> play 3
  State 7/8. Move disk 5; skipped none -> play 5
Level 4
  State 1/16. Move disk 7; skipped 2,3,5 -> play 30/7
  State 2/16. Move disk 5; skipped 2,3 -> play 12/5
  State 3/16. Move disk 7; skipped 2,3 -> play 24/7
  State 4/16. Move disk 3; skipped 2 -> play 8/3
  State 5/16. Move disk 7; skipped 3,5 -> play 30/7
  State 6/16. Move disk 5; skipped 2 -> play 16/5
  ...
```