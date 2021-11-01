insert into races
    (id, name, description, frequencyminutes, readydelayminutes, isenabled, minimumchao, maximumchao, prizerings,
    difficulty, swimpercentage, flypercentage, runpercentage, powerpercentage, intelligencepercentage, luckpercentage)
values
    (1, 'Crab Pool', 'Beginner race. Swimming is key to victory.', 30, 1, true, 1, 8, 100, 1, 0.75, 0.00, 0.25, 0.00, 0.00, 0.00),
    (2, 'Stump Valley', 'Beginner race. Flying is key to victory.', 30, 1, true, 1, 8, 100, 1, 0.13, 0.50, 0.25, 0.13, 0.00, 0.00),
    (3, 'Mushroom Forest', 'Beginner race. Running is key to victory.', 30, 1, true, 1, 8, 100, 1, 0.00, 0.00, 1.00, 0.00, 0.00, 0.00),
    (4, 'Block Canyon', 'Beginner race. Power is key to victory.', 30, 1, true, 1, 8, 100, 1, 0.00, 0.00, 0.14, 0.86, 0.00, 0.00),
    (5, 'Kalaupapa Volcano', 'Beginner race. Stamina is key to victory.', 30, 1, true, 1, 8, 100, 1, 0.00, 0.00, 1.00, 0.00, 0.00, 0.00),
    (6, 'Aquamarine', 'Intermediate race. Swimming is key to victory.', 60, 3, true, 1, 8, 200, 2, 0.35, 0.40, 0.61, 0.00, 0.00, 0.00),
    (7, 'Topaz', 'Intermediate race. Flying is key to victory.', 60, 3, true, 1, 8, 200, 2, 0.32, 0.16, 0.39, 0.13, 0.00, 0.00),
    (8, 'Peridot', 'Intermediate race. Running is key to victory.', 60, 3, true, 1, 8, 200, 2, 0.00, 0.00, 1.00, 0.00, 0.00, 0.00),
    (9, 'Garnet', 'Intermediate race. Power is key to victory.', 60, 3, true, 1, 8, 200, 2, 0.07, 0.07, 0.53, 0.33, 0.00, 0.00),
    (10, 'Onyx', 'Intermediate race. Intelligence and luck are key to victory.', 90, 5, true, 1, 8, 300, 3, 0.07, 0.02, 0.51, 0.00, 0.20, 0.20),
    (11, 'Diamond', 'Intermediate race. General ability is key to victory.', 120, 10, true, 1, 8, 400, 4, 0.15, 0.04, 0.43, 0.05, 0.16, 0.16);

insert into racesegments
    (id, raceid, description, raceindex, terraintype, startelevation, endelevation, terraindistance, staminalossmultiplier,
    terraindifficulty, intelligencerating, luckrating)
values
    --Crab Pool
    (1, 1, 'The race starts on a grassy ledge. There''s a short distance to the pool.', 0, 0, 10, 10, 10, 1.0, 0, 0, 0),
    (2, 1, 'The finish line awaits on the other side of a large pool. Crabs watch the chao as they swim.', 1, 1, 10, 0, 30, 1.0, 0, 0, 0),
    --Stump Valley
    (3, 2, 'A short trail leads from the starting line to a sudden cliff.', 0, 0, 40, 40, 10, 1.0, 0, 0, 0),
    (4, 2, 'The cliff overlooks a narrow river valley. If the chao can reach the far ledge, they can avoid falling into the water below.', 1, 1, 40, 0, 10, 1.0, 0, 0, 0),
    (5, 2, 'The chao that couldn''t reach the far ledge climb out of the water, making their way up...', 2, 2, 0, 10, 0, 1.0, 0, 0, 0),
    (6, 2, 'All that remains is a curved dirt path to the finish!', 3, 0, 10, 10, 10, 1.0, 0, 0, 0),
    --Mushroom Forest
    (7, 3, 'The finish line is visible at the top of the hill. The path is steep, but it''s just a simple foot race.', 0, 0, 0, 0, 50, 1.0, 0, 0, 0),
    --Block Canyon
    (8, 4, 'The cliff ahead is the only obstacle, but it''s a short distance away.', 0, 0, 0, 0, 5, 1.0, 0, 0, 0),
    (9, 4, 'All that remains is the tall cliff. Don''t look down!', 1, 2, 0, 30, 0, 1.0, 0, 0, 0),
    --Kalaupapa Volcano
    (10, 5, 'The cavernous path to the finish line spirals downward. Heat waves distort the air.', 0, 0, 0, 0, 30, 2.0, 0, 0, 0),
    --Aquamarine
    (11, 6, 'The path ahead crosses a river. It''s a short sprint to the water.', 0, 0, 15, 15, 10, 1.0, 0, 0, 0),
    (12, 6, 'The river flows steadily as the chao begin to cross.', 1, 1, 15, 10, 10, 1.0, 0, 0, 0),
    (13, 6, 'The chao enter a dark, winding tunnel.', 2, 0, 10, 10, 50, 1.0, 0, 0, 0),
    (14, 6, 'The crab pool lies below a grassy ledge. The chao dive in and begin to swim across.', 3, 1, 10, 0, 30, 1.0, 0, 0, 0),
    (15, 6, 'The last leg of the race is another stretch of trail. The finish line is straight ahead.', 4, 0, 0, 0, 10, 1.0, 0, 0, 0),
    --Topaz
    (16, 7, 'The starting path curves around to a low cliff. A ledge is visible above it.', 0, 0, 20, 20, 10, 1.0, 0, 0, 0),
    (17, 7, 'A narrow river valley separates the cliff from the upper ledge. The chao fly across, trying to avoid falling into the water below.', 1, 1, 20, 10, 10, 1.0, 0, 0, 0),
    (18, 7, 'The chao climb quickly up the ledge, unable to see what awaits them afterward.', 2, 2, 10, 30, 0, 1.0, 0, 0, 0),
    (19, 7, 'Atop the ledge, the path curves toward another cliff. It''s a short distance away.', 3, 0, 30, 30, 10, 1.0, 0, 0, 0),
    (20, 7, 'The cliff overlooks the same river valley, but the view is dizzying from up here. The chao try to glide over the river.', 4, 1, 30, 10, 10, 1.0, 0, 0, 0),
    (21, 7, 'The chao enter a dark, winding tunnel.', 5, 0, 10, 10, 50, 1.0, 0, 0, 0),
    (22, 7, 'The crab pool lies below a grassy ledge. The chao dive in and begin to swim across.', 6, 1, 10, 0, 30, 1.0, 0, 0, 0),
    (23, 7, 'The last leg of the race is another stretch of trail. The finish line is straight ahead.', 7, 0, 0, 0, 10, 1.0, 0, 0, 0),
    --Peridot
    (24, 8, 'The path goes up a steep hill into a cave. Everyone''s vying for first place.', 0, 0, 0, 0, 60, 1.0, 0, 0, 0),
    (25, 8, 'The path winds as it descends, emerging from the cave. It''s still a long way to the end.', 1, 0, 0, 0, 60, 1.0, 0, 0, 0),
    (26, 8, 'This is the final stretch! The finish line approaches fast.', 2, 0, 0, 0, 20, 1.0, 0, 0, 0),
    --Garnet
    (27, 9, 'The starting path curves around to a low cliff. A ledge is visible above it.', 0, 0, 20, 20, 10, 1.0, 0, 0, 0),
    (28, 9, 'A narrow river valley separates the cliff from the upper ledge. The chao fly across, trying to avoid falling into the water below.', 1, 1, 20, 10, 10, 1.0, 0, 0, 0),
    (29, 9, 'The chao climb quickly up the ledge, unable to see what awaits them afterward.', 2, 2, 10, 30, 0, 1.0, 0, 0, 0),
    (30, 9, 'Atop the ledge, there''s a long stretch of trail to reach the next obstacle, a cliff that looms in the distance.', 3, 0, 30, 30, 60, 1.0, 0, 0, 0),
    (31, 9, 'The tall ledge blocks the way to the end. Don''t look down!', 4, 2, 30, 60, 0, 1.0, 0, 0, 0),
    (32, 9, 'The last running section curves around to the finish line. The chao try to take the shortest path as they rapidly approach it.', 5, 0, 60, 60, 20, 1.0, 0, 0, 0),
    --Onyx
    (33, 10, 'Small boxes are lined up ahead. As the race starts, the chao dash toward them.', 0, 0, 0, 0, 10, 1.0, 0, 0, 0),
    (34, 10, 'The chao sit and wind up their boxes... and spooky toys spring out of some of them, startling the chao!', 1, 0, 0, 0, 0, 1.0, 0, 30, 30),
    (35, 10, 'Some chao are just getting up as others race toward a waterfall that spills over the path ahead.', 2, 0, 0, 0, 10, 1.0, 0, 0, 0),
    (36, 10, 'The surging water threatens to push the chao over the edge as they swim across. It''s a strong current!', 3, 1, 0, 0, 20, 1.0, 20, 0, 0),
    (37, 10, 'Leading into a tunnel, the path curves to the left...', 4, 0, 0, 0, 40, 1.0, 0, 0, 0),
    (38, 10, 'There''s a room full of different types of fruit. A large, glowing monitor displays one. The chao each choose one, but some choose poorly...', 5, 0, 0, 0, 0, 1.0, 0, 30, 0),
    (39, 10, 'The path winds as it descends, emerging from the cave. It''s still a long way to the end.', 6, 0, 0, 0, 70, 1.0, 0, 0, 0),
    (40, 10, 'This is the final stretch! The finish line approaches fast, but there are traps ahead! Some of the chao stumble into pitfalls at the very end.', 7, 0, 0, 0, 20, 1.0, 0, 0, 30),
    --Diamond
    (41, 11, 'Small boxes are lined up ahead. As the race starts, the chao dash toward them.', 0, 0, 20, 20, 10, 1.0, 0, 0, 0),
    (42, 11, 'The chao sit and wind up their boxes... and spooky toys spring out of some of them, startling the chao!', 1, 0, 20, 20, 0, 1.0, 0, 30, 30),
    (43, 11, 'Some chao are just getting up as others race around the bend toward the river valley ahead.', 2, 0, 20, 20, 15, 1.0, 0, 0, 0),
    (44, 11, 'A narrow river valley separates the cliff from the upper ledge. The chao fly across, trying to avoid falling into the water below.', 3, 1, 20, 10, 10, 1.0, 0, 0, 0),
    (45, 11, 'The chao climb quickly up the ledge, unable to see what awaits them afterward.', 4, 2, 10, 30, 0, 1.0, 0, 0, 0),
    (46, 11, 'The path curves ahead toward a waterfall that spills over the path.', 5, 0, 30, 30, 20, 1.0, 0, 0, 0),
    (47, 11, 'The surging water threatens to push the chao over the edge as they swim across. It''s a strong current!', 6, 1, 30, 30, 15, 1.0, 20, 0, 0),
    (48, 11, 'Leading into a tunnel, the path curves to the left...', 7, 0, 30, 30, 40, 1.0, 0, 0, 0),
    (49, 11, 'There''s a room full of different types of fruit. A large, glowing monitor displays one. The chao each choose one, but some choose poorly...', 8, 0, 30, 30, 0, 1.0, 0, 30, 0),
    (50, 11, 'The path spirals down, merging with another tunnel. The grassy ledge overlooking the crab pool lies at the bottom.', 9, 0, 30, 30, 40, 1.0, 0, 0, 0),
    (51, 11, 'The chao reach the crab pool, spring off the ledge, and begin to swim across.', 10, 1, 30, 20, 30, 1.0, 0, 0, 0),
    (52, 11, 'Reaching the far side of the pool, the chao take the short path around the upcoming bend in the race route.', 11, 0, 20, 20, 10, 1.0, 0, 0, 0),
    (53, 11, 'Though the straightaway is a long one, it''s all that remains. Pitfalls surprise some chao on their way, causing them to fall in or stumble.', 12, 0, 20, 20, 40, 1.0, 0, 0, 30);

update info set schema_version = 4;