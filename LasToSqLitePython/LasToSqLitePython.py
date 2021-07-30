from laspy import file as File
from laspy import header
import copy
import sqlite3
import os
import math


# Print iterations progress
def printProgressBar (iteration, total, prefix = '', suffix = '', decimals = 1, length = 100, fill = '█', printEnd = "\r"):
    """
    Call in a loop to create terminal progress bar
    @params:
        iteration   - Required  : current iteration (Int)
        total       - Required  : total iterations (Int)
        prefix      - Optional  : prefix string (Str)
        suffix      - Optional  : suffix string (Str)
        decimals    - Optional  : positive number of decimals in percent complete (Int)
        length      - Optional  : character length of bar (Int)
        fill        - Optional  : bar fill character (Str)
        printEnd    - Optional  : end character (e.g. "\r", "\r\n") (Str)
    """
    percent = ("{0:." + str(decimals) + "f}").format(100 * (iteration / float(total)))
    filledLength = int(length * iteration // total)
    bar = fill * filledLength + '-' * (length - filledLength)
    print(f'\r{prefix} |{bar}| {percent}% {suffix}', end = printEnd)
    # Print New Line on Complete
    if iteration == total: 
        print()


# Set to the location of the LAS file.
fileName = r"C:\Users\EngRafaelSMacedo\Desktop\00 CANOPY\Laser Scan Data\4.20.21\optimized.las"

# Creates - and opens the sqlite database
sqlite_filename = fileName + "_lasData.sqlite"
if (os.path.exists(sqlite_filename)): os.remove(sqlite_filename)
conn = sqlite3.connect(sqlite_filename)
cur = conn.cursor()

# Creates the destination table
cur.execute(r"CREATE TABLE [Points]([X] DOUBLE NOT NULL,[Y] DOUBLE NOT NULL,[Z] DOUBLE NOT NULL)")
conn.commit()

# Open an input file in read mode.
inFile = File.File(fileName,mode="r")

offsets = inFile.header.offset
scales = inFile.header.scale

pointcount = inFile.header.point_records_count
transmitlimit = 50000
inserts = pointcount // transmitlimit
if (pointcount % transmitlimit != 0):
    inserts = inserts + 1
insertCounter = 0
printProgressBar(0, inserts, prefix = 'Progress:', suffix = 'Complete', length = 50)

# Iterate over all of the available points
pointstoadd = []
for p in inFile:
    # Populates the X, Y and Z of the files
    p.make_nice() 
    # Adds the point to the temp array
    pointstoadd.append((p.X * scales[0] + offsets[0],p.Y * scales[1] + offsets[1],p.Z * scales[2] + offsets[2]))

    # Sends the package to sqlite
    if (len(pointstoadd) == transmitlimit):
        cur.executemany("INSERT INTO [Points](X,Y,Z) VALUES (?,?,?)", pointstoadd)
        conn.commit()
        pointstoadd.clear()
        # Update Progress Bar
        printProgressBar(insertCounter, inserts, prefix = 'Progress:', suffix = 'Complete', length = 50)
        insertCounter = insertCounter + 1

# Sends the last package if there is something to send
if (len(pointstoadd) > 0):
    conn.executemany("INSERT INTO [Points](X,Y,Z) VALUES (?,?,?)", pointstoadd)
    conn.commit()
    pointstoadd.clear
    printProgressBar(insertCounter, inserts, prefix = 'Progress:', suffix = 'Complete', length = 50)
    insertCounter = insertCounter + 1

print("Creating Indexes")
cur.execute(r"CREATE INDEX idx_X ON [Points](X)")
cur.execute(r"CREATE INDEX idx_Y ON [Points](Y)")
cur.execute(r"CREATE INDEX idx_Z ON [Points](Z)")
conn.commit()

# Closes the database connection
conn.close()

print("Done", flush = true)
