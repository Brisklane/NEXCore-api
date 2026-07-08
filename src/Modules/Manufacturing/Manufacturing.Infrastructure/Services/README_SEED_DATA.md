# Manufacturing Module - Comprehensive Test Data

## Overview
The Manufacturing module now includes comprehensive test data similar to the Inventory module, covering all major manufacturing entities and workflows.

## Test Data Coverage

### 1. Products (Simulated from Inventory Module)

#### Finished Goods - Furniture (5 items)
- **FG-001**: Office Chair
- **FG-002**: Standing Desk
- **FG-003**: Conference Table
- **FG-004**: Filing Cabinet
- **FG-005**: Bookshelf

#### Finished Goods - Electronics (3 items)
- **FG-010**: Laptop Stand
- **FG-011**: Monitor Arm
- **FG-012**: Mechanical Keyboard

#### Raw Materials - Metals (4 items)
- **RM-001**: Steel Tube
- **RM-002**: Aluminum Sheet
- **RM-003**: Brass Rod
- **RM-004**: Copper Wire

#### Raw Materials - Textiles & Foam (3 items)
- **RM-010**: Fabric Roll
- **RM-011**: Leather Sheet
- **RM-012**: Foam Sheet

#### Raw Materials - Wood (4 items)
- **RM-020**: Wood Panel
- **RM-021**: Plywood Sheet
- **RM-022**: MDF Board
- **RM-023**: Oak Timber

#### Raw Materials - Fasteners & Components (6 items)
- **RM-030**: Screws Pack
- **RM-031**: Nuts Pack
- **RM-032**: Bolts Pack
- **RM-033**: Washers Pack
- **RM-034**: Hinges
- **RM-035**: Door Handles

#### Raw Materials - Finishing (5 items)
- **RM-040**: Paint Can
- **RM-041**: Varnish
- **RM-042**: Lacquer
- **RM-043**: Primer
- **RM-044**: Sandpaper

#### Raw Materials - Plastic & Polymers (4 items)
- **RM-050**: Plastic Sheet
- **RM-051**: ABS Plastic
- **RM-052**: PVC Pipe
- **RM-053**: Rubber Sheet

#### Sub-Assemblies (5 items)
- **SA-001**: Chair Leg Assembly
- **SA-002**: Chair Armrest
- **SA-003**: Backrest Assembly
- **SA-010**: Drawer Unit
- **SA-011**: Cabinet Door

### 2. Work Centers (8 work centers)
- **WC-ASSEMBLY**: Assembly Line
- **WC-MACHINING**: Machining Center
- **WC-PAINTING**: Painting Station
- **WC-QC**: Quality Control
- **WC-PACKAGING**: Packaging Line
- **WC-WELDING**: Welding Station
- **WC-CUTTING**: Cutting Station
- **WC-SUBCON**: Sub-Contract

### 3. Work Center Shifts (7 shifts)
- Assembly Line shifts (Morning & Evening)
- Machining Center day shift
- Painting Station day shift
- QC full day shift
- Packaging day shift
- Welding morning shift

### 4. Bills of Material (5 BOMs)

#### BOM-001: Office Chair
- Steel Tube (4 PCS, 2% scrap)
- Fabric Roll (1.5 MTR, 1% scrap)
- Foam Sheet (0.8 KG)
- Screws Pack (1 PKT)
- Paint Can (0.25 LTR)
- **By-product**: Steel scrap

#### BOM-002: Standing Desk
- Wood Panel (4 PCS, 1% scrap)
- Steel Tube (2 PCS, 2% scrap)
- Screws Pack (2 PKT)
- Paint Can (0.5 LTR)

#### BOM-003: Conference Table
- Oak Timber (6 PCS, 1% scrap)
- Steel Tube (4 PCS, 2% scrap)
- Screws Pack (3 PKT)
- Varnish (1 LTR)
- Bolts Pack (2 PKT)

#### BOM-004: Filing Cabinet
- Steel Tube (8 PCS, 2% scrap)
- MDF Board (5 PCS, 1% scrap)
- Drawer Unit sub-assembly (4 PCS)
- Door Handles (4 PCS)
- Screws Pack (2 PKT)
- Paint Can (0.5 LTR)

#### BOM-005: Bookshelf
- Plywood Sheet (6 PCS, 1% scrap)
- Steel Tube (2 PCS, 1% scrap)
- Screws Pack (2 PKT)
- Varnish (0.5 LTR)
- Washers Pack (1 PKT)

### 5. Routings (5 routings with operations)

#### Routing-001: Chair Manufacturing (6 operations)
1. Cut & Bend Steel Tubes (Machining)
2. Weld Frame (Welding)
3. Paint Frame (Painting)
4. Assemble Seat & Back (Assembly)
5. Quality Inspection (QC)
6. Packaging & Labelling (Packaging)

#### Routing-002: Desk Manufacturing (5 operations)
1. Cut Wood Panels (Machining)
2. Assemble Desk Frame (Assembly)
3. Paint & Lacquer (Painting)
4. Final QC (QC)
5. Box & Wrap (Packaging)

#### Routing-003: Table Manufacturing (7 operations)
1. Cut Oak Panels (Machining)
2. Assemble Table Top (Assembly)
3. Weld Leg Frame (Welding)
4. Attach Legs to Top (Assembly)
5. Apply Varnish (Painting)
6. Final Inspection (QC)
7. Package for Delivery (Packaging)

#### Routing-004: Cabinet Manufacturing (9 operations)
1. Cut Steel Frame (Machining)
2. Weld Cabinet Frame (Welding)
3. Cut MDF Panels (Machining)
4. Assemble Cabinet Body (Assembly)
5. Install Drawers (Assembly)
6. Paint Cabinet (Painting)
7. Attach Handles (Assembly)
8. Quality Check (QC)
9. Package (Packaging)

#### Routing-005: Bookshelf Manufacturing (7 operations)
1. Cut Plywood Shelves (Machining)
2. Cut Steel Brackets (Machining)
3. Assemble Frame (Assembly)
4. Install Shelves (Assembly)
5. Apply Finish (Painting)
6. Inspection (QC)
7. Pack (Packaging)

### 6. Production Orders (5 orders in different states)

#### PO-2024-001: Office Chair (In Progress)
- **Quantity**: 100 (75 produced, 5 rejected)
- **Status**: InProgress
- **Progress**: Assembly 70% complete
- Includes:
  - 6 production order operations (3 completed, 1 in progress, 2 pending)
  - 5 component issues
  - Production schedules
  - Material issues
  - Work-in-progress tracking
  - Quality inspection (70 inspected, 68 passed, 2 rejected)
  - 5 inspection characteristics
  - Finished goods receipt (68 units)
  - Production batch (BTH-2024-001)
  - Cost entries (material, labor, machine, overhead)
  - Production variance analysis
  - Machine downtime record
  - Rework order for 2 rejected units

#### PO-2024-002: Standing Desk (Completed)
- **Quantity**: 40 (all completed)
- **Status**: Completed
- **Note**: Completed ahead of schedule
- Includes:
  - 5 production order operations (all completed)
  - 4 component issues
  - Material issues
  - Work-in-progress (completed)
  - Quality inspection (40 inspected, all passed)
  - 4 inspection characteristics
  - Finished goods receipt (40 units)
  - Production batch (BTH-2024-002)
  - Cost entries
  - Production variance (labor over-run)
  - Sub-contract order (painting outsourced)

#### PO-2024-003: Office Chair (Planned)
- **Quantity**: 50
- **Status**: Planned
- **Purpose**: Stock replenishment
- **Scheduled**: Days 5-22 from now

#### PO-2024-004: Conference Table (Planned)
- **Quantity**: 20
- **Status**: Planned
- **Purpose**: Office project
- **Scheduled**: Days 7-20 from now

#### PO-2024-005: Filing Cabinet (Draft)
- **Quantity**: 30
- **Status**: Draft
- **Purpose**: Warehouse storage
- **Scheduled**: Days 10-27 from now

### 7. Standard Costs (5 products)
- Office Chair: $85 (Material) + $30 (Labor) + $20 (Machine) + $17.50 (Overhead) = $152.50
- Standing Desk: $120 + $45 + $35 + $30 = $230
- Conference Table: $200 + $60 + $50 + $40 = $350
- Filing Cabinet: $150 + $50 + $40 + $30 = $270
- Bookshelf: $100 + $35 + $25 + $20 = $180

### 8. Material Planning Data (8 products)
- Finished goods: Safety stock, reorder points, max levels, lot sizes
- Raw materials: Lead times, procurement types, MRP types

### 9. Supporting Entities

#### Demands (2 records)
- Office Chair: 100 units (due in 30 days)
- Standing Desk: 40 units (due in 45 days)

#### Planned Orders (2 records)
- Converted to production orders (PO-2024-001 and PO-2024-002)

#### Capacity Loads (7 snapshots)
- Daily capacity tracking across work centers
- Utilization percentages (50% to 100%)
- Overload detection

#### Inventory Transactions (6 movements)
- Goods issues (raw materials)
- Goods receipts (finished goods)
- Scrap receipts

#### Inspections (2 records)
- PO-001: 70 inspected, 68 passed, 2 rejected
- PO-002: 40 inspected, all passed

#### Inspection Characteristics (9 measurements)
- Quantitative: Seat height, back rest angle, screw tightness, desktop flatness, height adjustment
- Qualitative: Paint finish, load test, leg stability, paint uniformity

#### Work-in-Progress (2 tracking records)
- Real-time WIP tracking for active production orders

#### Finished Goods Receipts (2 receipts)
- Warehouse receipts with batch numbers

#### Production Batches (2 batches)
- Batch/lot traceability
- Manufacturing dates, expiry dates
- Quality approval status
- Certificate of Analysis (COA)

#### Cost Entries (2 records)
- Detailed cost breakdown by category
- Posted to accounting

#### Production Variances (2 analyses)
- Standard vs actual cost comparison
- Variance categories (Material, Labor)
- Favorable/unfavorable analysis

#### Machine Downtime (1 incident)
- Welding torch blockage
- 1.5 hours downtime
- Resolution tracked

#### Rework Orders (1 order)
- 2 chairs with surface defects
- Scheduled for rework

#### Sub-Contract Orders (1 order)
- Painting outsourced for desk production
- Cost tracking
- Vendor management

### 10. Warehouses (3 warehouses)
- **WH-001**: Main Warehouse
- **WH-002**: Raw Materials Warehouse
- **WH-003**: Finished Goods Warehouse

## Usage

The seed data is automatically created when:
1. A new company is created in the system
2. The `ManufacturingSeedDataService.SeedAsync()` method is called
3. The method checks if data already exists to prevent duplication

## Test Scenarios Covered

1. ? **Complete Production Flow**: Chair manufacturing from start to finish
2. ? **Completed Orders**: Desk production completed ahead of schedule
3. ? **Planned Orders**: Future production planning
4. ? **Quality Control**: Inspections with pass/fail criteria
5. ? **Rework Management**: Handling defective products
6. ? **Sub-Contracting**: Outsourcing operations
7. ? **Cost Variance**: Standard vs actual cost analysis
8. ? **Capacity Planning**: Work center utilization tracking
9. ? **Machine Downtime**: OEE tracking
10. ? **Batch Traceability**: Lot tracking with COA
11. ? **Multi-Level BOMs**: Components and sub-assemblies
12. ? **Complex Routings**: Multi-operation manufacturing flows
13. ? **Material Planning**: MRP integration
14. ? **Inventory Integration**: Stock movements
15. ? **Overhead Allocation**: Manufacturing overhead tracking

## Comparison with Inventory Module

Similar to how the Inventory module seeds comprehensive product catalogs (beverages, dairy, food, electronics, etc.), the Manufacturing module now seeds:
- Comprehensive product range (furniture, electronics accessories)
- Extensive raw materials (metals, wood, textiles, plastics, etc.)
- Complete BOMs with realistic scrap percentages
- Detailed routing operations
- Multiple production order states (draft, planned, in-progress, completed)
- Full end-to-end manufacturing workflows

## Next Steps

To extend this further, you can:
1. Add more product categories (apparel, automotive parts, etc.)
2. Add more sub-assemblies and multi-level BOMs
3. Add more inspection characteristics
4. Add alternative routings
5. Add engineering change orders
6. Add more machine downtime scenarios
7. Add preventive maintenance records
8. Add tool/fixture management data

## Notes

- All GUIDs are generated at runtime to avoid conflicts
- The seed service is idempotent (won't create duplicates)
- Data follows realistic manufacturing workflows
- Cost calculations are realistic and balanced
- All entities are properly linked with foreign keys
