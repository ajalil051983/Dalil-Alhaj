import sqlite3

conn = sqlite3.connect(r'd:\Ai workspace\Khayrat Alhaj\DbBackup\appdata.bin.bak')
cur = conn.cursor()
cur.execute("SELECT name, sql FROM sqlite_master WHERE type='table'")
for row in cur.fetchall():
    print('TABLE:', row[0])
    print(row[1])
    print()

# Show sample data from each table
cur.execute("SELECT name FROM sqlite_master WHERE type='table'")
tables = [r[0] for r in cur.fetchall()]
for table in tables:
    print(f'\n--- Sample from {table} ---')
    cur.execute(f'SELECT * FROM {table} LIMIT 3')
    cols = [d[0] for d in cur.description]
    print('Columns:', cols)
    for row in cur.fetchall():
        print(row[:6])  # first 6 cols to avoid huge output

conn.close()
