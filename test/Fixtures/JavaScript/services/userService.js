class UserService {
    constructor() {
        this.users = [];
    }

    findById(id) {
        return this.users.find(u => u.id === id);
    }

    getAll() {
        return this.users.slice();
    }

    save(user) {
        this.users.push(user);
    }
}

function validateUser(user) {
    return user.name && user.email;
}

export { UserService, validateUser };
