db = db.getSiblingDB('GroceryDB');

db.GroceryProducts.insertMany([
    {
        Name: "Whole Milk",
        Price: 4.99,
        StockQuantity: 100,
        PasteurizationDate: new Date(new Date().setDate(new Date().getDate() - 2))
    },
    {
        Name: "Low Fat Milk",
        Price: 4.49,
        StockQuantity: 50,
        PasteurizationDate: new Date(new Date().setDate(new Date().getDate() - 5))
    },
    {
        Name: "Skim Milk",
        Price: 3.99,
        StockQuantity: 30,
        PasteurizationDate: new Date(new Date().setDate(new Date().getDate() - 20))
    }
]);

db.users.insertMany([
    {
        Username: "admin",
        PasswordHash: "$2a$11$yYy.F.U9nU45wQ6qG74/kueqG.jT6x1K6pXm.4s5X7gG.J3.hW7/q",
        Role: "Admin"
    },
    {
        Username: "user",
        PasswordHash: "$2a$11$61J2yN9.hG9/12.698gUHu.N8n8g.Y/U8a8163mB.n3.U.D7456d9",
        Role: "User"
    }
]);
