//
//  LC_Internal.mm
//  LiteCore
//
//  Created by Jens Alfke on 10/27/16.
//  Copyright © 2016 Couchbase. All rights reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file
//  except in compliance with the License. You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software distributed under the
//  License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,
//  either express or implied. See the License for the specific language governing permissions
//  and limitations under the License.

#import "LC_Internal.h"


bool convertError(const C4Error &c4err, NSError **outError) {
    NSCAssert(c4err.code != 0 && c4err.domain != 0, @"No C4Error");
    static NSString* const kDomains[] = {nil, @"LiteCore", NSPOSIXErrorDomain, @"ForestDB", @"SQLite"};
    if (outError) {
        auto msg = c4error_getMessage(c4err);
        NSString* msgStr = [[NSString alloc] initWithBytes: msg.buf length: msg.size
                                                  encoding: NSUTF8StringEncoding];
        *outError = [NSError errorWithDomain: kDomains[c4err.domain] code: c4err.code
                                    userInfo: @{NSLocalizedDescriptionKey: msgStr}];
    }
    return false;
}


bool convertError(const FLError &flErr, NSError **outError) {
    NSCAssert(flErr != 0, @"No C4Error");
    if (outError)
        *outError = [NSError errorWithDomain: FLErrorDomain code: flErr userInfo: nil];
    return false;
}
