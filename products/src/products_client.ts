export{}

const PROTO_PATH = __dirname + '/products.proto';

const grpc = require('@grpc/grpc-js');
const protoLoader = require('@grpc/proto-loader');

const packageDefinition = protoLoader.loadSync(
    PROTO_PATH,
    {keepCase: true,
     longs: String,
     enums: String,
     defaults: true,
     oneofs: true
    });

const products_proto = grpc.loadPackageDefinition(packageDefinition).products;

function main() {

 const target = 'localhost:50051';

  const client = new products_proto.Products(target, grpc.credentials.createInsecure());

  client.delete({id:2}, function(err, response) {
    if(err)
    {
        const {code, details}=err;
        return console.log({code, details});
    }

    console.log(response);
  });
  /*
  client.list({}, function(err, response) {
    if(err)
    {
        const {code, details}=err;
        return console.log({code, details});
    }
    console.log(response);
  });

  client.get({id:2}, function(err, response) {
    if(err)
    {
        const {code, details}=err;
        return console.log({code, details});
    }
    client.delete({id:2}, function(err, response) {
        if(err)
        {
            const {code, details}=err;
            return console.log({code, details});
        }
        client.list({}, function(err, response) {
            if(err)
            {
                const {code, details}=err;
                return console.log({code, details});
            }
            console.log(response);
          });
      });
  });
  */
}

main();
