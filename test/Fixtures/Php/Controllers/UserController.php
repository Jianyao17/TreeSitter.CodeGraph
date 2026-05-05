<?php

namespace App\Controllers;

require_once '../Services/UserService.php';

use App\Services\UserService;

class UserController
{
    private UserService $userService;

    public function __construct(UserService $userService)
    {
        $this->userService = $userService;
    }

    public function getUser(int $id): array
    {
        $user = $this->userService->findById($id);
        return $user;
    }

    public function getAllUsers(): array
    {
        return $this->userService->getAll();
    }

    public function createUser(string $name, string $email): void
    {
        $this->userService->save($name, $email);
    }

    public static function version(): string
    {
        return '1.0.0';
    }
}
