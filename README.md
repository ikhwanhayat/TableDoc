# TableDoc
Simple command line tool to generate documentation for MS SQL tables.

# Usage
```
TableDoc.exe "Data Source=(local);Initial Catalog=northwind;user id=usr;password=pwd" > output.html
```
This will create a file with a list of tables in the database specified in the connection string. 
For each table, its columns definition (Name, Type, Nullable) will be shown.
