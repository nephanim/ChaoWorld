insert into races
    (id, name, description, frequencyminutes, readydelayminutes, isenabled, minimumchao, maximumchao, prizerings,
    difficulty, swimpercentage, flypercentage, runpercentage, powerpercentage, intelligencepercentage, luckpercentage)
values
    (1, 'Crab Pool', 'Beginner race. Swimming is key to victory.', 1, 1, true, 1, 8, 100, 1, 0.75, 0.00, 0.25, 0.00, 0.00, 0.00),
    (2, 'Stump Valley', 'Beginner race. Flying is key to victory.', 1, 1, true, 1, 8, 100, 1, 0.13, 0.50, 0.25, 0.13, 0.00, 0.00),
    (3, 'Mushroom Forest', 'Beginner race. Running is key to victory.', 1, 1, true, 1, 8, 100, 1, 0.00, 0.00, 1.00, 0.00, 0.00, 0.00),
    (4, 'Block Canyon', 'Beginner race. Power is key to victory.', 1, 1, true, 1, 8, 100, 1, 0.00, 0.00, 0.08, 0.92, 0.00, 0.00),
    (5, 'Kalaupapa Volcano', 'Beginner race. Stamina is key to victory.', 1, 1, true, 1, 8, 100, 1, 0.00, 0.00, 1.00, 0.00, 0.00, 0.00),
    (6, 'Aquamarine', 'Intermediate race. Swimming is key to victory.', 2, 2, true, 1, 8, 150, 2, 0.59, 0.04, 0.37, 0.00, 0.00, 0.00),
    (7, 'Topaz', 'Intermediate race. Flying is key to victory.', 2, 2, true, 1, 8, 150, 2, 0.24, 0.33, 0.29, 0.14, 0.00, 0.00),
    (8, 'Peridot', 'Intermediate race. Running is key to victory.', 2, 2, true, 1, 8, 150, 2, 0.00, 0.00, 1.00, 0.00, 0.00, 0.00),
    (9, 'Garnet', 'Intermediate race. Power is key to victory.', 2, 2, true, 1, 8, 150, 2, 0.05, 0.05, 0.45, 0.45, 0.00, 0.00),
    (10, 'Onyx', 'Intermediate race. Intelligence and luck are key to victory.', 3, 3, true, 1, 8, 200, 3, 0.07, 0.02, 0.51, 0.00, 0.20, 0.20),
    (11, 'Diamond', 'Intermediate race. General ability is key to victory.', 4, 4, true, 1, 8, 250, 4, 0.14, 0.04, 0.42, 0.08, 0.16, 0.16),
    (12, 'River Run', 'Expert race. Swimming and flying are key to victory.', 5, 5, true, 1, 8, 300, 5, 0.35, 0.29, 0.04, 0, 0.18, 0.14),
   	(13, 'Egg Tower', 'Expert race. Power is key to victory.', 1, 1, false, 1, 8, 300, 5, 0, 0.16, 0.04, 0.56, 0.06, 0.18),
    (14, 'Windy Valley', 'Expert race. Flying and intelligence are key to victory.', 5, 5, false, 1, 8, 0, 5, 0, 0.63, 0, 0, 0.38, 0),
    (15, 'Obstacle Course', 'Intermediate race. Intelligence and luck are key to victory.', 1, 1, false, 1, 8, 600, 4, 0.13, 0.03, 0.12, 0.08, 0.32, 0.32);

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
    (9, 4, 'All that remains is the tall cliff. Don''t look down!', 1, 2, 0, 60, 0, 1.0, 0, 0, 0),
    --Kalaupapa Volcano
    (10, 5, 'The cavernous path to the finish line spirals downward. Heat waves distort the air.', 0, 0, 0, 0, 30, 2.0, 0, 0, 0),
    --Aquamarine
    (11, 6, 'The path ahead crosses a river. It''s a short sprint to the water.', 0, 0, 15, 15, 10, 1.0, 0, 0, 0),
    (12, 6, 'The river flows steadily as the chao begin to cross.', 1, 1, 15, 10, 10, 1.0, 0, 0, 0),
    (13, 6, 'The chao enter a dark, winding tunnel.', 2, 0, 10, 10, 30, 1.0, 0, 0, 0),
    (14, 6, 'The crab pool lies below a grassy ledge. The chao dive in and begin to swim across. Buoys mark a winding route through the pool.', 3, 1, 10, 0, 70, 1.0, 0, 0, 0),
    (15, 6, 'The last leg of the race is another stretch of trail. The finish line is straight ahead.', 4, 0, 0, 0, 10, 1.0, 0, 0, 0),
    --Topaz
    (16, 7, 'The starting path curves around to a low cliff. A ledge is visible above it.', 0, 0, 40, 40, 10, 1.0, 0, 0, 0),
    (17, 7, 'A narrow river valley separates the cliff from the upper ledge. The chao fly across, trying to avoid falling into the water below.', 1, 1, 40, 30, 10, 1.0, 0, 0, 0),
    (18, 7, 'The chao climb quickly up the ledge, unable to see what awaits them afterward.', 2, 2, 30, 60, 0, 1.0, 0, 0, 0),
    (19, 7, 'Atop the ledge, the path curves toward another cliff. It''s a short distance away.', 3, 0, 60, 60, 10, 1.0, 0, 0, 0),
    (20, 7, 'The cliff overlooks the same river valley, but the view is dizzying from up here. The chao try to glide over the river.', 4, 1, 60, 10, 10, 1.0, 0, 0, 0),
    (21, 7, 'The chao enter a dark, winding tunnel.', 5, 0, 10, 10, 30, 1.0, 0, 0, 0),
    (22, 7, 'The crab pool lies below a grassy ledge. The chao dive in and begin to swim across.', 6, 1, 10, 0, 30, 1.0, 0, 0, 0),
    (23, 7, 'The last leg of the race is another stretch of trail. The finish line is straight ahead.', 7, 0, 0, 0, 10, 1.0, 0, 0, 0),
    --Peridot
    (24, 8, 'The path goes up a steep hill into a cave. Everyone''s vying for first place.', 0, 0, 0, 0, 60, 1.0, 0, 0, 0),
    (25, 8, 'The path winds as it descends, emerging from the cave. It''s still a long way to the end.', 1, 0, 0, 0, 60, 1.0, 0, 0, 0),
    (26, 8, 'This is the final stretch! The finish line approaches fast.', 2, 0, 0, 0, 20, 1.0, 0, 0, 0),
    --Garnet
    (27, 9, 'The starting path curves around to a low cliff. A ledge is visible above it.', 0, 0, 20, 20, 10, 1.0, 0, 0, 0),
    (28, 9, 'A narrow river valley separates the cliff from the upper ledge. The chao fly across, trying to avoid falling into the water below.', 1, 1, 20, 10, 10, 1.0, 0, 0, 0),
    (29, 9, 'The chao climb quickly up the ledge, unable to see what awaits them afterward.', 2, 2, 10, 40, 0, 1.0, 0, 0, 0),
    (30, 9, 'Atop the ledge, there''s a long stretch of trail to reach the next obstacle, a cliff that looms in the distance.', 3, 0, 40, 40, 60, 1.0, 0, 0, 0),
    (31, 9, 'The tall ledge blocks the way to the end. Don''t look down!', 4, 2, 40, 100, 0, 1.0, 0, 0, 0),
    (32, 9, 'The last running section curves around to the finish line. The chao try to take the shortest path as they rapidly approach it.', 5, 0, 100, 100, 20, 1.0, 0, 0, 0),
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
    (45, 11, 'The chao climb quickly up the ledge, unable to see what awaits them afterward.', 4, 2, 10, 40, 0, 1.0, 0, 0, 0),
    (46, 11, 'The path curves ahead toward a waterfall that spills over the path.', 5, 0, 40, 40, 20, 1.0, 0, 0, 0),
    (47, 11, 'The surging water threatens to push the chao over the edge as they swim across. It''s a strong current!', 6, 1, 40, 40, 15, 1.0, 20, 0, 0),
    (48, 11, 'Leading into a tunnel, the path curves to the left...', 7, 0, 40, 40, 40, 1.0, 0, 0, 0),
    (49, 11, 'There''s a room full of different types of fruit. A large, glowing monitor displays one. The chao each choose one, but some choose poorly...', 8, 0, 40, 40, 0, 1.0, 0, 30, 0),
    (50, 11, 'The path spirals down, merging with another tunnel. The grassy ledge overlooking the crab pool lies at the bottom.', 9, 0, 40, 40, 40, 1.0, 0, 0, 0),
    (51, 11, 'The chao reach the crab pool, spring off the ledge, and begin to swim across.', 10, 1, 40, 30, 30, 1.0, 0, 0, 0),
    (52, 11, 'Reaching the far side of the pool, the chao take the short path around the upcoming bend in the race route.', 11, 0, 30, 30, 10, 1.0, 0, 0, 0),
    (53, 11, 'Though the straightaway is a long one, it''s all that remains. Pitfalls surprise some chao on their way, causing them to fall in or stumble.', 12, 0, 30, 30, 40, 1.0, 0, 0, 30),
    --River Run
    (54, 12, 'The chao all sprint toward the cliff ahead.', 0, 0, 150, 150, 10, 1.0, 0, 0, 0),
    (55, 12, 'Flying out of Block Canyon, the river valley sparkles below. The course bends to the right toward a waterfall that spills over the ground.', 1, 0, 150, 90, 10, 1.0, 0, 0, 0),
    (56, 12, 'The chao plunge into the water, following the current toward the edge of the waterfall...', 2, 1, 90, 90, 10, 1.0, 20, 0, 0),
    (57, 12, 'The valley closes in around them as the chao glide down from the waterfall into the winding river.', 3, 1, 90, 40, 30, 1.0, 0, 0, 0),
    (58, 12, 'The river current begins to pick up. Boulders ahead divide it into three sections, forcing the chao to choose which route to take.', 4, 1, 40, 40, 20, 2.0, 20, 0, 30),
    (59, 12, 'After weaving between the rocks, there is another waterfall. Intense rapids lie below. The chao glide over the dangerous waters as far as they can.', 5, 1, 40, 10, 50, 3.0, 30, 0, 0),
    (60, 12, 'The rapids continue. Some chao struggle to keep their heads above the surface, while others are finding the fastest current through to safer waters.', 6, 1, 10, 10, 30, 3.0, 30, 90, 0),
    (61, 12, 'The river calms as it flows into a humid cave, then splits into multiple unmarked tunnels. The chao choose their routes carefully.', 7, 1, 10, 10, 20, 1.0, 0, 0, 40),
    (62, 12, 'The river tunnels come back together just before the flags of the finish line, arching over the river.', 8, 1, 10, 10, 10, 1.0, 0, 0, 0),
    --Egg Tower
    (63, 13, 'The abandoned facility looms over the chao as they begin climbing the outer wall. A faint humming can be heard from within.', 0, 2, 0, 55, 0, 1.0, 0, 0, 0),
	(64, 13, 'The chao climb onto a ledge of rusted plate metal. Graffiti paints the surrounding walls. As they approach a console ahead, floor hatches open beneath them, revealing a series of high-speed warp tubes that suck the chao in.', 1, 0, 55, 55, 20, 1.0, 0, 0, 15),
	(65, 13, 'The chao flail as they are transported through the interior of the facility. They are dumped together at the base of a vertical shaft and begin climbing up.', 2, 2, 55, 135, 0, 1.0, 0, 0, 30),
	(66, 13, 'As the chao reach the top of the interior column, they enter a large, steamy chamber that stinks of rotten eggs. The cage of an unknown device rotates slowly around them, and through gaps in its metal frame, they observe a sea of bubbling liquid.', 3, 2, 135, 195, 0, 2.0, 0, 0, 0),
	(67, 13, 'The chao gradually realize the spinning metal arms are the only safe way up, but the moat of bubbling liquid stands in the way. They each start to glide across the gap. Those that miss their targets are momentarily trapped in the hot pool before grasping onto safety.', 4, 1, 195, 115, 50, 5.0, 0, 0, 45),
	(68, 13, 'The device continues to spin at a dizzying speed as the chao scale its arms. Where they meet at the top, the chao enter another narrow vertical shaft filled with vile-smelling steam. The finish line waits at the top!', 5, 2, 115, 195, 0, 3.0, 0, 0, 0),
    --Windy Valley
    (69, 14, 'Surrounded by the whistle of strong winds, the chao are gathered atop a grassy plateau. The ground far below it is hidden by mist and raging winds that carry debris with it. They get a running start toward the edge of the plateau.', 0, 0, 500, 500, 10, 1.0, 0, 0, 0),
	(70, 14, 'Taking flight, the chao glide over and between other rocky spires nearby as they make their way toward the goal banner far ahead in the distance.', 1, 0, 500, 0, 60, 10.0, 0, 0, 0),
	(71, 14, 'A tornado rolls through their path, its mighty winds causing some chao to fly astray. They adjust their course amidst the peril and realign with their destination.', 2, 0, 0, 0, 60, 10.0, 0, 60, 0),
	(72, 14, 'The winds seem calm up in the air, but they continue to rage down below. From glimpses through the mist, the terrain is nearly impassable.', 3, 0, 0, 0, 60, 10.0, 0, 0, 0),
	(73, 14, 'A powerful gust swirls from beneath the chao and then shoots skyward. While the wind in their sails may be welcome, the sheer force causes the chao to spiral out of control for a moment as they study the winds.', 4, 0, 0, 0, 60, 10.0, 0, 60, 0),
	(74, 14, 'Just a short distance remains to the finish line located in a grassy clearing surrounded by thick forest. The racing chao ride one another''s slipstreams as they vie for first place.', 5, 0, 0, 0, 60, 10.0, 0, 0, 0),
    --Obstacle Course
    (75, 15, 'An artificial climbing wall waits at the start of the race. Everyone begins scaling it, searching for the right path as other chao block their way.', 0, 2, 0, 30, 0, 1.0, 0, 80, 0),
    (76, 15, 'Atop the wall is a wooden suspension bridge. As the chao proceed, they realize the walls have cannons hidden inside. Plungers fire at them from left and right. The chao try to avoid not only the plungers, but their fallen competition.', 1, 0, 30, 30, 25, 1.0, 0, 60, 100),
    (77, 15, 'Ropes allow the chao to swing down to a track below. As they leap forward, they glide a short distance to the ground. The track is riddled with hidden pitfalls that pull some of them under.', 2, 0, 30, 20, 25, 1.0, 0, 0, 40),
    (78, 15, 'The last stretch is a swimming pool full of floating mines. These explode when touched, sometimes causing chain reactions. The chao are tossed about as they try to navigate this final obstacle.', 3, 1, 20, 20, 50, 1.0, 0, 100, 100);

update info set schema_version = 4;