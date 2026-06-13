UPDATE "Products" SET "ImageUrl" =
  CASE
    WHEN "Category" = 'Cement'    THEN 'https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=600'
    WHEN "Category" = 'Steel Bar' THEN 'https://images.unsplash.com/photo-1587293852726-70cdb56c2866?w=600'
    WHEN "Category" = 'Brick'     THEN 'https://images.unsplash.com/photo-1504307651254-35680f356dfd?w=600'
    WHEN "Category" = 'Sand'      THEN 'https://images.unsplash.com/photo-1614164185128-e4ec99c436d7?w=600'
    WHEN "Category" = 'Paint'     THEN 'https://images.unsplash.com/photo-1562259929-b4e1fd3aef09?w=600'
    WHEN "Category" = 'Tile'      THEN 'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=600'
    WHEN "Category" = 'Other'     THEN 'https://images.unsplash.com/photo-1621905252507-b35492cc74b4?w=600'
    ELSE "ImageUrl"
  END
WHERE "ImageUrl" IS NULL;
