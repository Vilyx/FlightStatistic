set DIR=%1..\GameData\OLDD\FlightStatistic\



if not exist %DIR% mkdir %DIR%
copy FlightStatistic.dll %DIR%
copy FlightStatistic.pdb %DIR%


cd %1..
call test.bat
