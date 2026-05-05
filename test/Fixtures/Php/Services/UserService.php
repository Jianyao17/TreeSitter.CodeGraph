<?php

namespace App\Services;

class UserService
{
    private array $users = [];

    public function __construct()
    {
    }

    public function findById(int $id): array
    {
        return $this->users[$id] ?? [];
    }

    public function getAll(): array
    {
        return $this->users;
    }

    public function save(string $name, string $email): void
    {
        $this->users[] = ['name' => $name, 'email' => $email];
    }
}
