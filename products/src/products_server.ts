export { }

const PROTO_PATH = __dirname + '/products.proto';

import * as grpc from '@grpc/grpc-js';
import * as protoLoader from '@grpc/proto-loader';
import * as sqlite3 from 'sqlite3';
const sqlite = sqlite3.verbose();

const packageDefinition = protoLoader.loadSync(
    PROTO_PATH,
    {
        keepCase: true,
        longs: String,
        enums: String,
        defaults: true,
        oneofs: true
    });

interface ProductsGrpcObject {
    Products: any
}

const products_proto = grpc.loadPackageDefinition(packageDefinition).products as grpc.GrpcObject|ProductsGrpcObject;

const dbName = 'products.db';

function list(call, callback) {
    console.log('List');

    const db = new sqlite3.Database(dbName, (err) => {
        if (err) {
            console.error(err.message);
            return callback({
                code: 400,
                message: err.message,
                status: grpc.status.INTERNAL
              })
        }
    });
    
    db.all('SELECT id, name FROM products ORDER BY id', [], (err, rows) => {
        if (err) {
            console.error(err.message);
            return callback({
                code: 400,
                message: err.message,
                status: grpc.status.INTERNAL
              })
        }

        callback(null, { products: rows });
    }).close((err) => {
        if (err) {
            console.error(err.message);
        }
    });
}

function get(call, callback) {
    console.log('Get');

    const db = new sqlite3.Database(dbName, (err) => {
        if (err) {
            console.error(err.message);
            return callback({
                code: 400,
                message: err.message,
                status: grpc.status.INTERNAL
              })
        }
    });

    db.get('SELECT id, name FROM products WHERE id  = ?', [call.request.id], (err, row) => {
        if (err) {
            console.error(err.message);
            return callback({
                code: 400,
                message: err.message,
                status: grpc.status.INTERNAL
              })
        }

        if(!row?.id) {
            return callback({
                code: 400,
                message: `No product found with id ${call.request.id}`,
                status: grpc.status.NOT_FOUND
              })
        }

        callback(null, row);
    }).close((err) => {
        if (err) {
        return console.error(err.message);
        }
    });
}

function create(call, callback) {
    console.log('Get');
    callback(null, { id: 1, name: 'Keyboard' });
}

function update(call, callback) {
    console.log('Get');
    callback(null, { id: 1, name: 'Keyboard' });
}

function deleteProduct(call, callback) {
    console.log('Delete');
    const db = new sqlite3.Database(dbName, (err) => {
        if(err)
        {
            console.error(err?.message);
            return callback({
                code: 400,
                message: err?.message,
                status: grpc.status.INTERNAL
            });
        }
    });

    db.get('SELECT id, name FROM products WHERE id  = ?', [call.request.id], (err, row) => {
        if (err) {
            console.error(err.message);
            return callback({
                code: 400,
                message: err.message,
                status: grpc.status.INTERNAL
                });
        }

        if(!row?.id) {
            return callback({
                code: 400,
                message: `No product found with id ${call.request.id}`,
                status: grpc.status.NOT_FOUND
              })
        }

        db.run('DELETE FROM products WHERE id = ?', [row.id], function (err) {
            if (err) {
                console.error(err.message);
                return callback({
                    code: 400,
                    message: err.message,
                    status: grpc.status.INTERNAL
                    });
            }

            callback(null, row);
            console.log(`${this.changes} products have been deleted`);
        });
    })

    db.close((err) => {
        if (err) {
            return console.error(err.message);
        }
    });
}

function seedSqlite() {
    const db = new sqlite3.Database(dbName, (err) => {
        console.error(err?.message);
    });
    
    db.serialize(() => {
        db.run('DROP TABLE IF EXISTS products')
        .run('CREATE TABLE products(id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, name TEXT)')
        .run('INSERT INTO products(name) VALUES (?), (?), (?)', ['Mouse', 'Keyboard', 'Display'], function (err) {
            if (err){
                throw err;
            }
            console.log(`Products have been inserted, last rowid is ${this.lastID}`);
        });
    });
    
    db.close((err) => {
        if (err) {
        return console.error(err.message);
        }
    });
}

function main() {
    seedSqlite();

    const target = '0.0.0.0:50051';
    const server = new grpc.Server();
    server.addService(products_proto.Products.service, { list, get, create, update, delete: deleteProduct });
    server.bindAsync(target, grpc.ServerCredentials.createInsecure(), () => {
        server.start();
    });

    console.log(`Server started on ${target}`);
}

main();
