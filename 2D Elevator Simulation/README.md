# 🏢 2D Elevator Simulation

A highly optimized, scalable 2D elevator simulation built in **Unity 6**. This project was designed to demonstrate advanced C# architecture, efficient memory management, and intelligent algorithmic routing.

Unlike standard elevator simulations that simply move objects between points, this project features a **fully autonomous AI passenger system**, a custom **load-balancing dispatcher**, and a strict **Event-Driven Architecture**.

## ✨ Core Features

* **Multi-Elevator Dispatching:** 3 independent elevators flawlessly servicing 4 floors simultaneously.
* **Smart Load Balancing:** The central dispatcher uses a custom `GetEtaCost` algorithm. It doesn't just look for the closest elevator—it heavily penalizes busy elevators, perfectly spreading the load across idle elevators to maximize efficiency.
* **Autonomous AI Passengers:** Passengers physically spawn, walk to wait points using `SmoothDamp`, board available slots, and exit autonomously. 
* **Smooth Easing Physics:** Elevators use custom mathematical easing curves to accelerate and decelerate smoothly (no robotic linear movement or instant teleportation).
* **Stress-Test Ready:** Includes a custom Inspector tool to trigger a "Burst Spawn" of 20+ passengers simultaneously to prove system stability under heavy load.

## 🏗️ Technical Architecture Highlights

This project was built strictly adhering to Senior-level developer best practices:

* **Event-Driven Architecture (Zero Polling):** Instead of using heavy `Update()` loops to scan for open elevators, the AI completely sleeps while waiting. Elevators broadcast `Action` events (`OnElevatorDoorsOpened`) that passengers instantly subscribe and react to, saving massive CPU overhead.
* **Object Pooling:** Passenger AI agents are fully pooled (`Queue<Passenger>`). The spawner reuses deactivated passengers rather than calling `Instantiate()`/`Destroy()` or `GetComponent()`, ensuring **zero Garbage Collection spikes** during runtime.
* **Decoupled Systems (SRP):** 
  * `ElevatorSystemManager`: Strictly handles lobby math and load balancing.
  * `ElevatorController`: Strictly handles localized physics, movement easing, and its internal request queue.
  * `Passenger`: Strictly handles its own state machine (`Waiting`, `Boarding`, `Riding`).
* **Strict Encapsulation:** Zero "magic numbers" in the codebase. All variables are firmly encapsulated with `[SerializeField] private` and accessible only via read-only C# Properties.

## 🎮 How to Run

1. Open the project in **Unity 6**.
2. Open the main Scene.
3. Hit **Play**. 
4. The system will automatically spawn passengers. You can also manually adjust the **Rand Bar (Spawn Interval)** in the `PassengerSpawner` Inspector, or click the **Trigger Burst Now** checkbox to stress-test the load balancer!
