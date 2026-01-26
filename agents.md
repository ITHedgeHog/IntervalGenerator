# Smart Meter Interval Generator

## Overview

The Smart Meter Interval Generator is a .NET application designed to generate realistic interval-based energy consumption data for modeling and testing purposes. This tool simulates smart meter readings at configurable intervals (15-minute or 30-minute) for various types of commercial and industrial facilities.

## Purpose

This repository provides a flexible interval data generation system that can:

- **Model Different Business Types**: Generate consumption patterns specific to offices, manufacturing plants, retail facilities, and other commercial entities
- **Support Multiple Interval Periods**: Create data at 15-minute or 30-minute intervals to match real-world smart meter configurations
- **Deterministic and Non-Deterministic Modes**:
  - **Deterministic Mode**: Generate repeatable, predictable patterns using seeded random number generation for testing and validation
  - **Non-Deterministic Mode**: Create realistic, variable consumption patterns for simulation and modeling purposes

## Use Cases

1. **Testing Energy Management Systems**: Validate energy monitoring software with realistic test data
2. **Load Forecasting Development**: Create training datasets for predictive models
3. **Billing System Validation**: Test energy billing calculations with known consumption patterns
4. **API Development**: Generate sample data for energy data APIs
5. **Performance Testing**: Create large volumes of interval data for stress testing data pipelines

## Key Features

- Configurable interval periods (15min, 30min)
- Business-type specific consumption profiles
- Seasonal and time-of-day variations
- Weekend vs. weekday pattern differentiation
- Deterministic seed-based generation for reproducibility
- Extensible architecture for custom consumption patterns

## Business Type Profiles

The generator supports modeling different facility types with characteristic consumption patterns:

- **Office Buildings**: Business hours peak usage, reduced weekend consumption
- **Manufacturing Plants**: Continuous or shift-based operation patterns
- **Retail Facilities**: Extended hours with peak periods
- **Data Centers**: Consistent baseline with controlled variations
- **Educational Institutions**: Term-time variations and scheduled occupancy

## Technology

- **.NET**: Core framework for application development
- **C#**: Primary programming language
- **Extensible Design**: Plugin architecture for custom consumption models
