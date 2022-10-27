import * as grpc from '@grpc/grpc-js';
import * as jwt from 'jsonwebtoken';
import * as protoLoader from '@grpc/proto-loader';
import * as sqlite3 from 'sqlite3';

const sqlite = sqlite3.verbose();

const PROTO_PATH = __dirname + '/products.proto';

const packageDefinition = protoLoader.loadSync(
    PROTO_PATH,
    {
        keepCase: true,
        longs: Number,
        enums: String,
        defaults: true,
        oneofs: true
    });

interface ProductsGrpcObject {
    Products: any
}

const products_proto = grpc.loadPackageDefinition(packageDefinition).products as grpc.GrpcObject | ProductsGrpcObject;

const dbName = 'products.db';
const jwtSignKey = process.env.TOKEN_KEY;
const issuer = process.env.TOKEN_ISSUER||'https://github.com/vyatkin0/micro-services';
const audience = process.env.TOKEN_AUDIENCE||'https://github.com/vyatkin0/micro-services';

const claimMsRole = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

function authCall(call, callback, roles: string[] = []) {
    const authorizations = call.metadata.get('authorization');
    for (const auth of authorizations) {
        const bearer = 'Bearer ';
        if (auth.startsWith(bearer)) {
            const decoded = jwt.verify(auth.substring(bearer.length), jwtSignKey, { audience, issuer });
            console.log(decoded);
            if (decoded[claimMsRole])
            {
                if(Array.isArray(decoded[claimMsRole])
                    && decoded[claimMsRole].some(r => ['Admin', ...roles].includes(r))
                    || ['Admin', ...roles].includes(decoded[claimMsRole]))
                {
                    return decoded;
                }
            }

            callback({
                code: grpc.status.PERMISSION_DENIED,
                message: 'Unauthorized',
            });

            throw new Error('Unauthorized');
        }
    }
}

function list(call, callback) {
    console.log('List');

    authCall(call, callback, ['User']);

    const db = new sqlite.Database(dbName);

    db.all('SELECT id, name FROM products ORDER BY id', [], (err, rows) => {
        if (err) {
            console.error(err.message);
            return callback({
                code: grpc.status.INTERNAL,
                message: err.message,
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
    authCall(call, callback, ['User']);

    const db = new sqlite.Database(dbName);

    db.get('SELECT id, name FROM products WHERE id  = ?', [call.request.id], (err, row) => {
        if (err) {
            console.error(err.message);
            return callback({
                code: grpc.status.INTERNAL,
                message: err.message,
            })
        }

        if (!row?.id) {
            return callback({
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
    console.log('Create');
    authCall(call, callback);

    const db = new sqlite.Database(dbName);

    db.run('INSERT INTO products(name) VALUES (?)', [call.request.name], function (err) {
        if (err) {
            console.error(err.message);
            return callback({
                code: grpc.status.INTERNAL,
                message: err.message,
            });
        }
        console.log(`${this.changes} products have been created`);

        db.get('SELECT id, name FROM products WHERE id=?', [this.lastID], function (err, row) {
            if (err) {
                console.error(err.message);
                return callback({
                    code: grpc.status.INTERNAL,
                    message: err.message,
                });
            }

            if (!row?.id) {
                return callback({
                    message: `No product found with id ${call.request.id}`,
                    status: grpc.status.NOT_FOUND
                })
            }

            callback(null, row);
        });
    }).close((err) => {
        if (err) {
            return console.error(err.message);
        }
    });
}

function update(call, callback) {
    console.log('Update');
    authCall(call, callback);

    const db = new sqlite.Database(dbName);

    db.serialize(() => {
        db.run('UPDATE products SET name=? WHERE id=?', [call.request.name, call.request.id], function (err) {
            if (err) {
                console.error(err.message);
                return callback({
                    code: grpc.status.INTERNAL,
                    message: err.message,
                });
            }
            console.log(`${this.changes} products have been updated`);
        })
            .get('SELECT id, name FROM products WHERE id=?', [call.request.id], function (err, row) {
                if (err) {
                    console.error(err.message);
                    return callback({
                        code: grpc.status.INTERNAL,
                        message: err.message,
                    });
                }

                if (!row?.id) {
                    return callback({
                        message: `No product found with id ${call.request.id}`,
                        status: grpc.status.NOT_FOUND
                    })
                }

                callback(null, row);
            });
    });

    db.close((err) => {
        if (err) {
            return console.error(err.message);
        }
    });
}

function deleteProduct(call, callback) {
    console.log('Delete');
    authCall(call, callback);
    const db = new sqlite.Database(dbName);

    db.get('SELECT id, name FROM products WHERE id  = ?', [call.request.id], (err, row) => {
        if (err) {
            console.error(err.message);
            return callback({
                code: grpc.status.INTERNAL,
                message: err.message,
            });
        }

        if (!row?.id) {
            return callback({
                message: `No product found with id ${call.request.id}`,
                status: grpc.status.NOT_FOUND
            })
        }

        db.run('DELETE FROM products WHERE id = ?', [row.id], function (err) {
            if (err) {
                console.error(err.message);
                return callback({
                    code: grpc.status.INTERNAL,
                    message: err.message,
                });
            }

            callback(null, row);
            console.log(`${this.changes} products have been deleted`);
        });
    }).close((err) => {
        if (err) {
            return console.error(err.message);
        }
    });
}

function seedSqlite() {
    const db = new sqlite.Database(dbName);

    db.serialize(() => {
        db.run('DROP TABLE IF EXISTS products')
            .run('CREATE TABLE products(id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, name TEXT)')
            .run('INSERT INTO products(name) VALUES (?), (?), (?), (?), (?)', ['Notebook', 'Monitor', 'Mouse', 'Keyboard', 'Phone'], function (err) {
                if (err) {
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
    const target = `0.0.0.0:${process.env.PORT||8080}`;
    const server = new grpc.Server();
    server.addService(products_proto.Products.service, { list, get, create, update, delete: deleteProduct });

    server.bindAsync(target, grpc.ServerCredentials.createInsecure(), () => {
        seedSqlite();
        server.start();
    });

    console.log(`Server started on ${target}`);
}

main();
